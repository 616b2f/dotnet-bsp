using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using MsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using TestResult = bsp4csharp.Protocol.TestResult;
using BaseProtocol.Protocol;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetTest)]
internal partial class BuildTargetTestHandler
    : IRequestHandler<TestParams, TestResult, RequestContext>
{
    private readonly IInitializeManager<InitializeBuildParams, InitializeBuildResult> _capabilitiesManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildTargetTestHandler(
        IInitializeManager<InitializeBuildParams, InitializeBuildResult> capabilitiesManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _capabilitiesManager = capabilitiesManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => false;

    public Task<TestResult> HandleRequestAsync(TestParams testParams, RequestContext context, CancellationToken cancellationToken)
    {
        var testResult = true;
        var initParams = _capabilitiesManager.GetInitializeParams();
        var projectTargets = new Dictionary<string, List<string>>();
        if (initParams.RootUri.IsFile)
        {
            var projects = new ProjectCollection();
            foreach (var target in testParams.Targets)
            {
                var fileExtension = Path.GetExtension(target.Uri.ToString());
                context.Logger.LogInformation("Target file extension {}", fileExtension);
                if (fileExtension == ".sln")
                {
                    var slnFile = SolutionFile.Parse(target.Uri.ToString());

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
                        var proj = projects.LoadProject(projectFile);
                        if (!projectTargets.ContainsKey(proj.FullPath))
                        {
                            projectTargets[proj.FullPath] = new List<string>();
                        }
                    }
                }
                else if (fileExtension == ".csproj")
                {
                    var proj = projects.LoadProject(target.Uri.ToString());
                    if (!projectTargets.ContainsKey(proj.FullPath))
                    {
                        projectTargets[proj.FullPath] = new List<string>();
                    }
                }
                else if (fileExtension == ".cs")
                {
                    var projectPath = ProjectFinder.FindProjectForTarget(target.Uri.ToString());
                    var proj = projects.LoadProject(projectPath);
                    if (!projectTargets.ContainsKey(proj.FullPath))
                    {
                        projectTargets[proj.FullPath] = new List<string>();
                    }
                    projectTargets[proj.FullPath].Add(target.Uri.ToString().Replace("file://", ""));
                }
            }

            var workspacePath = initParams.RootUri.AbsolutePath;
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

                var results = RunAllTests(proj, [targetPath], projectTarget.Value, context, msBuildLogger);
                var logMessgeParams = new LogMessageParams
                {
                    MessageType = MessageType.Log,
                    Message = JsonConvert.SerializeObject(results)
                };

                var _ = _baseProtocolClientManager.SendNotificationAsync(
                    Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);

                foreach (var result in results)
                {
                    if (result.ErrorMessage is not null)
                    {
                        WriteDiagnostic(result);
                        testResult = false;
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

    private void WriteDiagnostic(MsTestResult result)
    {

        var diagParams = new PublishDiagnosticsParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = UriFixer.WithFileSchema(result.TestCase.CodeFilePath ?? "") },
            BuildTarget = new BuildTargetIdentifier { Uri = result.TestCase.ExecutorUri },
        };

        var key = string.Concat(diagParams.TextDocument.Uri, "|", diagParams.BuildTarget.Uri);
        // diagParams.Reset = !_diagnosticKeysCollection.Contains(key);
        // if (diagParams.Reset)
        // {
        //     _diagnosticKeysCollection.Add(key);
        // }

        var testCase = result.TestCase;
        diagParams.Diagnostics.Add(new Diagnostic
        {
            Range = new bsp4csharp.Protocol.Range
            {
                Start = new Position { Line = testCase.LineNumber , Character = 0 },
                End = new Position { Line = testCase.LineNumber, Character = 0 }
            },
            Message = string.Format("{0}\n{1}", result.Messages, result.ErrorStackTrace),
            Code = result.TestCase.CodeFilePath,
            Source = testCase.Source,
            Severity = DiagnosticSeverity.Error
        });
        var _ = _baseProtocolClientManager.SendNotificationAsync(
            Methods.BuildPublishDiagnostics, diagParams, CancellationToken.None);
    }

    private List<MsTestResult> RunAllTests(Project proj, IEnumerable<string> targets, List<string> sourceFiles, RequestContext context, MSBuildLogger msBuildLogger)
    {
        context.Logger.LogInformation("Restore and build test target: {}", proj.ProjectFileLocation);
        var buildSuccess = proj.Build(["Restore", "Build"], [msBuildLogger]);

        if (!buildSuccess)
        {
            context.Logger.LogError("Restore or Build failed");
            return [];
        }

        var outputPath = proj.Properties.First(x => x.Name == "OutputPath").EvaluatedValue;
        context.Logger.LogInformation("outputPath: {}", outputPath);

        var assemblyName = proj.Properties.First(x => x.Name == "AssemblyName").EvaluatedValue;
        context.Logger.LogInformation("assemblyName: {}", assemblyName);

        var runnerLocation = TestRunner.FindVsTestConsole();

        if (runnerLocation is null)
        {
            context.Logger.LogError("Failed to find vstest.console.dll.");
            return [];
        }

        var testAdapterPath = TestRunner.FindTestAdapter(proj, context);

        if (testAdapterPath is null)
        {
            context.Logger.LogError("Failed to find any testadapter.");
            return [];
        }

        context.Logger.LogInformation("RunnerLocation: {}", runnerLocation);
        context.Logger.LogInformation("TestAdapter: {}", testAdapterPath);

        // IVsTestConsoleWrapper consoleWrapper = new VsTestConsoleWrapper(runnerLocation, new ConsoleParameters { LogFilePath = logFilePath });
        IVsTestConsoleWrapper consoleWrapper = new VsTestConsoleWrapper(runnerLocation);

        consoleWrapper.StartSession();
        consoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath });

        var waitHandle = new AutoResetEvent(false);
        var defaultRunSettings = "<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";

        var runHandler = new RunEventHandler(waitHandle);

        if (sourceFiles.Count > 0)
        {
            var discoveryHandler = new DiscoveryEventHandler(waitHandle);
            consoleWrapper.DiscoverTests(targets, defaultRunSettings, discoveryHandler);
            context.Logger.LogInformation("test cases: {}", JsonConvert.SerializeObject(discoveryHandler.DiscoveredTestCases));
            var testCases = discoveryHandler.DiscoveredTestCases
                .Where(x =>
                    x.CodeFilePath is not null &&
                    sourceFiles.Contains(x.CodeFilePath)
                );
            // var testOptions = new TestPlatformOptions { TestCaseFilter= "FullyQualifiedName=UnitTestProject.UnitTest.PassingTest" };
            context.Logger.LogInformation("Run test cases: {}", JsonConvert.SerializeObject(testCases));
            consoleWrapper.RunTests(testCases, defaultRunSettings, runHandler);
        }
        else
        {
            context.Logger.LogInformation("Run test targets: {}", targets);
            consoleWrapper.RunTests(targets, defaultRunSettings, runHandler);
        }

        waitHandle.WaitOne();
        return runHandler.TestResults;
    }

}