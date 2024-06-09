using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using TestResult = bsp4csharp.Protocol.TestResult;
using BaseProtocol.Protocol;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetTest)]
internal class BuildTargetTestHandler
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
                        projects.LoadProject(projectFile);
                    }
                }
                else if (fileExtension == ".csproj")
                {
                    projects.LoadProject(target.Uri.ToString());
                }
            }

            var workspacePath = initParams.RootUri.AbsolutePath;
            context.Logger.LogInformation("GetLoadedProjects from {}", workspacePath);
            _baseProtocolClientManager.SendClearDiagnosticsMessage();
            foreach (var proj in projects.LoadedProjects)
            {
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, workspacePath, proj.FullPath);
                var results = RunAllTests(proj, context, msBuildLogger);
                foreach (var result in results)
                {
                    var logMessgeParams = new LogMessageParams
                    {
                        MessageType = MessageType.Log,
                        Message = JsonConvert.SerializeObject(result)
                    };

                    var _ = _baseProtocolClientManager.SendNotificationAsync(
                        Methods.BuildLogMessage, logMessgeParams, CancellationToken.None);

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
            StatusCode = testResult ? StatusCode.Ok : StatusCode.Error
        });
    }

    private void WriteDiagnostic(MsTestResult result)
    {

        var diagParams = new PublishDiagnosticsParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = UriWithSchema(result.TestCase.CodeFilePath ?? "") },
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

    private List<MsTestResult> RunAllTests(Project proj, RequestContext context, MSBuildLogger msBuildLogger)
    {
        context.Logger.LogInformation("Restore and build test target: {}", proj.ProjectFileLocation);
        var buildSuccess = proj.Build(["Restore", "Build"], [msBuildLogger]);

        if (!buildSuccess)
        {
            context.Logger.LogError("Restore or Build failed");
            return [];
        }

        context.Logger.LogInformation("Start test target: {}", proj.ProjectFileLocation);
        var targetPath = proj.Properties.First(x => x.Name == "TargetPath").EvaluatedValue;
        context.Logger.LogInformation("targetPath: {}", targetPath);

        var outputPath = proj.Properties.First(x => x.Name == "OutputPath").EvaluatedValue;
        context.Logger.LogInformation("outputPath: {}", outputPath);

        var assemblyName = proj.Properties.First(x => x.Name == "AssemblyName").EvaluatedValue;
        context.Logger.LogInformation("assemblyName: {}", assemblyName);

        var runnerLocation = FindVsTestConsole();

        if (runnerLocation is null)
        {
            context.Logger.LogError("Failed to find vstest.console.dll.");
            return [];
        }

        var testAdapterPath = FindTestAdapter(proj, context);

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

        consoleWrapper.DiscoverTests([targetPath], defaultRunSettings, new DiscoveryEventHandler(waitHandle));

        var runHandler = new RunEventHandler(waitHandle);

        context.Logger.LogInformation("Run test target: {}", targetPath);
        consoleWrapper.RunTests([targetPath], defaultRunSettings, runHandler);

        waitHandle.WaitOne();
        return runHandler.TestResults;
    }

    private record SdkVersion(int Major, int Minor, int Patch, string DirPath);

    private string? FindVsTestConsole()
    {
        var userDir = Environment.ExpandEnvironmentVariables("%HOME%/.dotnet");
        string[] dirs = [
            userDir,
            "/usr/lib/dotnet/sdk",
            "/usr/lib64/dotnet/sdk",
            "/usr/share/dotnet/sdk"
        ];

        var versions = new List<SdkVersion>();
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var sdkdir in Directory.GetDirectories(dir))
            {
                var rex = new Regex(@"(\d*)\.(\d*)\.(\d*)");
                var match = rex.Match(sdkdir);
                if (match.Success)
                {
                    versions.Add(
                        new SdkVersion(
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value),
                            int.Parse(match.Groups[3].Value),
                            sdkdir
                        )
                    );
                }
            }
        }

        var highestVersion = versions
            .OrderByDescending(x => x.Major)
            .ThenByDescending(x => x.Minor)
            .ThenByDescending(x => x.Patch)
            .FirstOrDefault();

        if (highestVersion is not null)
        {
            return Path.Combine(highestVersion.DirPath, "vstest.console.dll");
        }

        return null;
    }

    private string? FindTestAdapter(Project proj, RequestContext context)
    {
        var targetPath = proj.Properties.First(x => x.Name == "TargetPath").EvaluatedValue;
        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (targetDirectory is null)
        {
            context.Logger.LogError("can't get root directory of target: {}", targetPath);
            return null;
        }

        var testAdapterPath = Directory.GetFiles(targetDirectory)
            .FirstOrDefault(x => x.EndsWith(".testadapter.dll", StringComparison.InvariantCultureIgnoreCase));

        if (testAdapterPath is null)
        {
            context.Logger.LogError("Can't find testadapter in path: {}", targetDirectory);
            return null;
        }

        context.Logger.LogInformation("test adapter found: {}", testAdapterPath);
        return testAdapterPath;
    }

    public class RunEventHandler : ITestRunEventsHandler
    {
        private AutoResetEvent waitHandle;

        public List<MsTestResult> TestResults { get; private set; }

        public RunEventHandler(AutoResetEvent waitHandle)
        {
            this.waitHandle = waitHandle;
            this.TestResults = new List<MsTestResult>();
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Console.WriteLine("Run Message: " + message);
        }

        public void HandleTestRunComplete(
            TestRunCompleteEventArgs testRunCompleteArgs,
            TestRunChangedEventArgs? lastChunkArgs,
            ICollection<AttachmentSet>? runContextAttachments,
            ICollection<string>? executorUris)
        {
            if (lastChunkArgs != null && lastChunkArgs.NewTestResults != null)
            {
                this.TestResults.AddRange(lastChunkArgs.NewTestResults);
            }

            Console.WriteLine("TestRunComplete");
            waitHandle.Set();
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            if (testRunChangedArgs != null && testRunChangedArgs.NewTestResults != null)
            {
                this.TestResults.AddRange(testRunChangedArgs.NewTestResults);
            }
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No op
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            // No op
            return -1;
        }
    }

    public class DiscoveryEventHandler : ITestDiscoveryEventsHandler
    {
        private AutoResetEvent waitHandle;

        public List<TestCase> DiscoveredTestCases { get; private set; }

        public DiscoveryEventHandler(AutoResetEvent waitHandle)
        {
            this.waitHandle = waitHandle;
            this.DiscoveredTestCases = new List<TestCase>();
        }

        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            Console.WriteLine("Discovery: " + discoveredTestCases.FirstOrDefault()?.DisplayName);

            if (discoveredTestCases != null)
            {
                this.DiscoveredTestCases.AddRange(discoveredTestCases);
            }
        }

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
        {
            if (lastChunk != null)
            {
                this.DiscoveredTestCases.AddRange(lastChunk);
            }

            Console.WriteLine("DiscoveryComplete");
            waitHandle.Set();
        }

        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            Console.WriteLine("Discovery Message: " + message);
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No op
        }
    
    }

    private Uri UriWithSchema(string path)
    {
        // workaround for "file://" schema being not serialized: https://github.com/dotnet/runtime/issues/90140
        return new Uri($"file://{path}", UriKind.Absolute);
    }
}