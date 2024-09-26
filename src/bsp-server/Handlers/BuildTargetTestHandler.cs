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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

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
                        HandleProject(testParams, projects, projectFile, projectTargets);
                    }
                }
                else if (fileExtension == ".csproj")
                {
                    HandleProject(testParams, projects, target.Uri.ToString().Replace("file://", ""), projectTargets);
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

                RunAllTests(proj, [targetPath], testParams.OriginId, projectTarget.Value, context, msBuildLogger);
            }
        }

        return Task.FromResult(new TestResult
        {
            OriginId = testParams.OriginId,
            StatusCode = testResult ? StatusCode.Ok : StatusCode.Error
        });
    }

    private void HandleProject(TestParams testParams, ProjectCollection projects, string projectFile, Dictionary<string, List<string>> projectTargets)
    {
        var proj = projects.LoadProject(projectFile);
        if (!projectTargets.ContainsKey(proj.FullPath))
        {
            projectTargets[proj.FullPath] = new List<string>();

            if (testParams.DataKind == TestParamsDataKinds.DotnetTest &&
                testParams.Data is JObject)
            {
                var dotnetTestParamsData = ((JObject)testParams.Data).ToObject<DotnetTestParamsData>();
                if (dotnetTestParamsData is not null)
                {
                    projectTargets[proj.FullPath].AddRange(dotnetTestParamsData.Filters);
                }
            }
        }
    }

    private void RunAllTests(Project proj, IEnumerable<string> targets, string? originId, List<string> testCaseFilters, RequestContext context, MSBuildLogger msBuildLogger)
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
        var defaultRunSettings = "<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";

        var buildTargetIdentifier = new BuildTargetIdentifier
        {
            Uri = UriFixer.WithFileSchema(proj.FullPath)
        };
        var runHandler = new TestRunEventHandler(waitHandle, originId, buildTargetIdentifier, _baseProtocolClientManager);

        if (testCaseFilters.Count > 0)
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