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
        var projectTargets = new Dictionary<string, JObject?>();
        if (initParams.RootUri.IsFile)
        {
            var projects = new ProjectCollection();
            foreach (var target in testParams.Targets)
            {
                var fileExtension = Path.GetExtension(target.ToString());
                context.Logger.LogInformation("Target file extension {}", fileExtension);
                if (fileExtension == ".sln")
                {
                    var slnFile = SolutionFile.Parse(target.ToString());

                    var configurationName = slnFile.GetDefaultConfigurationName();
                    var platformName = slnFile.GetDefaultPlatformName();
                    if (string.Equals(platformName, "Any CPU", StringComparison.InvariantCultureIgnoreCase))
                    {
                        platformName = "AnyCpu";
                    }

                    context.Logger.LogInformation("use platformName: {}", platformName);
                    context.Logger.LogInformation("use configurationName: {}", configurationName);
                    var projectFilesInSln = slnFile.ProjectsInOrder
                        .Where(x => 
                            (x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat ||
                            x.ProjectType == SolutionProjectType.WebProject) &&
                            // and only projects that has the build flag enabled for the provided configuration
                            x.ProjectConfigurations.Values.Any(v =>
                                v.ConfigurationName.Equals(configurationName, StringComparison.InvariantCultureIgnoreCase) &&
                                v.PlatformName.Equals(platformName, StringComparison.InvariantCultureIgnoreCase) &&
                                v.IncludeInBuild)
                            )
                        .Select(x => x.AbsolutePath);

                    foreach (var projectFile in projectFilesInSln)
                    {
                        HandleProject(testParams, projects, projectFile, projectTargets);
                    }
                }
                else if (fileExtension == ".csproj")
                {
                    HandleProject(testParams, projects, target.ToString(), projectTargets);
                }
            }

            var workspacePath = initParams.RootUri.LocalPath;
            _baseProtocolClientManager.SendClearDiagnosticsMessage();
            var testProjects = projects.LoadedProjects.Where(x => x.IsTestProject());
            context.Logger.LogInformation("Get loaded test projects from {}", workspacePath);
            foreach (var projectTarget in projectTargets)
            {
                var proj = projects.LoadProject(projectTarget.Key);
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, testParams.OriginId, workspacePath, proj.FullPath);

                context.Logger.LogInformation("Start test target: {}", proj.ProjectFileLocation);
                var targetPath = proj.Properties.First(x => x.Name == "TargetPath").EvaluatedValue;
                context.Logger.LogInformation("targetPath: {}", targetPath);

                var dotnetTestParamsData = projectTarget.Value?.ToObject<DotnetTestParamsData>();
                if (dotnetTestParamsData is not null)
                {
                    RunAllTests(proj, [targetPath], testParams.OriginId, dotnetTestParamsData.RunSettings, dotnetTestParamsData.Filters, context, msBuildLogger);
                }
                else
                {
                    RunAllTests(proj, [targetPath], testParams.OriginId, null, [], context, msBuildLogger);
                }
            }
        }

        return Task.FromResult(new TestResult
        {
            OriginId = testParams.OriginId,
            StatusCode = testResult ? StatusCode.Ok : StatusCode.Error
        });
    }

    private void HandleProject(TestParams testParams, ProjectCollection projects, string projectFile, Dictionary<string, JObject> projectTargets)
    {
        var proj = projects.LoadProject(projectFile);
        if (!projectTargets.ContainsKey(proj.FullPath))
        {
            if (testParams.DataKind == TestParamsDataKinds.DotnetTest &&
                testParams.Data is JObject testData)
            {
                projectTargets[proj.FullPath] = testData;
            }
        }
    }

    private void RunAllTests(Project proj, IEnumerable<string> targets, string? originId, string? testRunSettings, string[] testCaseFilters, RequestContext context, MSBuildLogger msBuildLogger)
    {
        context.Logger.LogInformation("Restore and build test target: {}", proj.ProjectFileLocation);
        var buildSuccess = proj.Build(["Restore", "Build"], [msBuildLogger]);

        if (!buildSuccess)
        {
            context.Logger.LogError("Restore or Build failed");
            return;
        }

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

        var waitHandle = new AutoResetEvent(false);
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
        var runHandler = new TestRunEventHandler(waitHandle, originId, buildTargetIdentifier, _baseProtocolClientManager);

        if (testCaseFilters?.Length > 0)
        {
            var filter = string.Join("|", testCaseFilters);
            var testOptions = new TestPlatformOptions { TestCaseFilter = filter };
            context.Logger.LogInformation("Run test cases with filters: {}", filter);
            consoleWrapper.RunTests(targets, defaultRunSettings, testOptions, runHandler);
        }
        else
        {
            context.Logger.LogInformation("Run test targets: {}", targets);
            consoleWrapper.RunTests(targets, defaultRunSettings, runHandler);
        }

        waitHandle.WaitOne();
        consoleWrapper.EndSession();
    }

}