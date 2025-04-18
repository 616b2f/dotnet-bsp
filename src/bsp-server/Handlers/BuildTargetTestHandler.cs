using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using TestResult = bsp4csharp.Protocol.TestResult;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Newtonsoft.Json.Linq;
using dotnet_bsp.EventHandlers;
using System.Text.RegularExpressions;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetTest)]
internal partial class BuildTargetTestHandler
    : IRequestHandler<TestParams, TestResult, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildTargetTestHandler(
        BuildInitializeManager initializeManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _initializeManager = initializeManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => false;

    public Task<TestResult> HandleRequestAsync(TestParams testParams, RequestContext context, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialized();

        var testResult = true;
        var initParams = _initializeManager.GetInitializeParams();

        if (initParams.RootUri.IsFile)
        {
            var projectTargets = new Dictionary<string, JObject?>();

            var testTargetFiles = BuildHelper
                .FilterProjectsOutIfPartOfAnSolutionTarget(testParams.Targets)
                .Select(x => x.ToString());

            var projects = new ProjectCollection();
            foreach (var target in testTargetFiles)
            {
                HandleProject(testParams, projects, target, projectTargets);
            }

            var workspacePath = initParams.RootUri.LocalPath;
            context.Logger.LogInformation("Get loaded test projects from {}", workspacePath);

            _baseProtocolClientManager.SendClearDiagnosticsMessage();

            var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, testParams.OriginId, workspacePath);
            testResult &= BuildHelper.RestoreTestTargets(
                testTargetFiles,
                projects,
                context.Logger,
                msBuildLogger);

            if (testResult)
            {
                msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, testParams.OriginId, workspacePath);
                testResult &= BuildHelper.BuildTestTargets(
                    testTargetFiles,
                    projects,
                    context.Logger,
                    msBuildLogger);
            }

            if (testResult)
            {
                foreach (var projectTarget in projectTargets)
                {
                    var proj = projects.LoadProject(projectTarget.Key);
                    msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, testParams.OriginId, workspacePath);

                    context.Logger.LogInformation("Start test target: {}", proj.ProjectFileLocation);
                    var targetPath = proj.Properties.First(x => x.Name == "TargetPath").EvaluatedValue;
                    context.Logger.LogInformation("targetPath: {}", targetPath);

                    var dotnetTestParamsData = projectTarget.Value?.ToObject<DotnetTestParamsData>();
                    if (dotnetTestParamsData is not null)
                    {
                        RunAllTests(proj, [targetPath], testParams.OriginId, dotnetTestParamsData.RunSettings, dotnetTestParamsData.Filter, context, msBuildLogger);
                    }
                    else
                    {
                        RunAllTests(proj, [targetPath], testParams.OriginId, null, string.Empty, context, msBuildLogger);
                    }
                }
            }
        }

        return Task.FromResult(new TestResult
        {
            OriginId = testParams.OriginId,
            StatusCode = testResult ? StatusCode.Ok : StatusCode.Error
        });
    }

    private void HandleProject(TestParams testParams, ProjectCollection projects, string projectFile, Dictionary<string, JObject?> projectTargets)
    {
        var proj = projects.LoadProject(projectFile);
        if (!projectTargets.ContainsKey(proj.FullPath))
        {
            JObject? testData = null;
            if (testParams.DataKind == TestParamsDataKinds.DotnetTest &&
                testParams.Data is JObject data)
            {
                testData = data;
            }

            projectTargets[proj.FullPath] = testData;
        }
    }

    private void RunAllTests(Project proj, IEnumerable<string> targets, string? originId, string? testRunSettings, string testCaseFilter, RequestContext context, MSBuildLogger msBuildLogger)
    {
        var outputPath = proj.Properties.First(x => x.Name == "OutputPath").EvaluatedValue;
        context.Logger.LogInformation("outputPath: {}", outputPath);

        var assemblyName = proj.Properties.First(x => x.Name == "AssemblyName").EvaluatedValue;
        context.Logger.LogInformation("assemblyName: {}", assemblyName);

        var runnerLocation = TestRunner.FindVsTestConsole();

        if (runnerLocation is null)
        {
            context.Logger.LogError("Failed to find vstest.console.dll.");
            return;
        }

        var testAdapterPath = TestRunner.FindTestAdapter(proj, context);

        if (testAdapterPath is null)
        {
            context.Logger.LogError("Failed to find any testadapter.");
            return;
        }

        context.Logger.LogInformation("RunnerLocation: {}", runnerLocation);
        context.Logger.LogInformation("TestAdapter: {}", testAdapterPath);

        IVsTestConsoleWrapper consoleWrapper = new VsTestConsoleWrapper(runnerLocation);

        consoleWrapper.StartSession();
        consoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath });

        var defaultRunSettings =
            """
            <RunSettings>
                <RunConfiguration>
                    <BatchSize>1500</BatchSize>
                </RunConfiguration>
            </RunSettings>
            """;

        var runSettings = testRunSettings ?? defaultRunSettings;

        var buildTargetIdentifier = new BuildTargetIdentifier
        {
            Uri = UriFixer.WithFileSchema(proj.FullPath)
        };
        if (!string.IsNullOrEmpty(testCaseFilter))
        {
            var waitHandle = new AutoResetEvent(false);
            var discoveryHandler = new TestDiscoveryEventHandler(waitHandle, buildTargetIdentifier, originId, _baseProtocolClientManager);
            consoleWrapper.DiscoverTests(targets, defaultRunSettings, discoveryHandler);
            waitHandle.WaitOne();

            var matchedTestCases = MatchTestCasesByFilter(testCaseFilter, context, discoveryHandler);

            waitHandle = new AutoResetEvent(false);
            var runHandler = new TestRunEventHandler(waitHandle, originId, buildTargetIdentifier, _baseProtocolClientManager);
            consoleWrapper.RunTests(matchedTestCases, defaultRunSettings, runHandler);
            waitHandle.WaitOne();
        }
        else
        {
            var waitHandle = new AutoResetEvent(false);
            var runHandler = new TestRunEventHandler(waitHandle, originId, buildTargetIdentifier, _baseProtocolClientManager);
            context.Logger.LogInformation("Run test targets: {}", targets);
            consoleWrapper.RunTests(targets, defaultRunSettings, runHandler);
            waitHandle.WaitOne();
        }

        consoleWrapper.EndSession();
    }

    private static IEnumerable<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase> MatchTestCasesByFilter(string testCaseFilter, RequestContext context, TestDiscoveryEventHandler discoveryHandler)
    {
        var testCaseIds = new List<Guid>();
        var regex = new Regex("^id==(.*)$");
        var match = regex.Match(testCaseFilter);
        if (match.Success)
        {
            var val = match.Groups[1].Value;
            if (!string.IsNullOrEmpty(val) &&
                Guid.TryParse(val, out Guid testCaseId))
            {
                testCaseIds.Add(testCaseId);
            }
        }
        context.Logger.LogInformation("Test case IDs found in filter: {}", string.Join(",", testCaseIds));

        var matchedTestCases = discoveryHandler.DiscoveredTestCases
            .Where(x => testCaseIds.Contains(x.Id));
        var rawIds = string.Join(",", matchedTestCases
            .Select(x => x.Id.ToString()));
        context.Logger.LogInformation("Run test cases with IDs: {}", rawIds);
        return matchedTestCases;
    }
}