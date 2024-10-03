using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetTestCaseDiscovery)]
internal partial class BuildTargetTestCaseDiscoveryHandler
    : IRequestHandler<TestCaseDiscoveryParams, TestCaseDiscoveryResult, RequestContext>
{
    private readonly IInitializeManager<InitializeBuildParams, InitializeBuildResult> _capabilitiesManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildTargetTestCaseDiscoveryHandler(
        IInitializeManager<InitializeBuildParams, InitializeBuildResult> capabilitiesManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _capabilitiesManager = capabilitiesManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => false;

    public Task<TestCaseDiscoveryResult> HandleRequestAsync(TestCaseDiscoveryParams testCaseDiscoveryParams, RequestContext context, CancellationToken cancellationToken)
    {
        var testCaseDiscoveryResult = true;
        var initParams = _capabilitiesManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var projects = new ProjectCollection();
            foreach (var target in testCaseDiscoveryParams.Targets)
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
                        var proj = projects.LoadProject(projectFile);
                    }
                }
                else if (fileExtension == ".csproj")
                {
                    var proj = projects.LoadProject(target.ToString());
                }
            }

            var workspacePath = initParams.RootUri.AbsolutePath;
            _baseProtocolClientManager.SendClearDiagnosticsMessage();
            var testProjects = projects.LoadedProjects.Where(x => x.IsTestProject());
            context.Logger.LogInformation("Get loaded test projects from {}", workspacePath);
            foreach (var proj in testProjects)
            {
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, testCaseDiscoveryParams.OriginId, workspacePath, proj.FullPath);

                context.Logger.LogInformation("Start test target: {}", proj.ProjectFileLocation);
                var targetPath = proj.Properties.First(x => x.Name == "TargetPath").EvaluatedValue;
                context.Logger.LogInformation("targetPath: {}", targetPath);

                var result = RunTestDiscovery(testCaseDiscoveryParams.OriginId, proj, [targetPath], context, msBuildLogger);

                if (!result)
                {
                    testCaseDiscoveryResult = result;
                }
            }
        }

        return Task.FromResult(new TestCaseDiscoveryResult
        {
            OriginId = testCaseDiscoveryParams.OriginId,
            StatusCode = testCaseDiscoveryResult ? StatusCode.Ok : StatusCode.Error
        });
    }

    private void HandleProject(TestCaseDiscoveryParams testParams, ProjectCollection projects, string projectFile, Dictionary<string, List<string>> projectTargets)
    {
        var proj = projects.LoadProject(projectFile);
        if (!projectTargets.ContainsKey(proj.FullPath))
        {
            projectTargets[proj.FullPath] = new List<string>();
        }
    }

    private bool RunTestDiscovery(string? originId, Project proj, IEnumerable<string> targets, RequestContext context, MSBuildLogger msBuildLogger)
    {
        context.Logger.LogInformation("Restore and build test target: {}", proj.ProjectFileLocation);
        var buildSuccess = proj.Build(["Restore", "Build"], [msBuildLogger]);

        if (!buildSuccess)
        {
            context.Logger.LogError("Restore or Build failed");
            return false;
        }

        var outputPath = proj.Properties.First(x => x.Name == "OutputPath").EvaluatedValue;
        context.Logger.LogInformation("outputPath: {}", outputPath);

        var assemblyName = proj.Properties.First(x => x.Name == "AssemblyName").EvaluatedValue;
        context.Logger.LogInformation("assemblyName: {}", assemblyName);

        var runnerLocation = TestRunner.FindVsTestConsole();

        if (runnerLocation is null)
        {
            context.Logger.LogError("Failed to find vstest.console.dll.");
            return false;
        }

        var testAdapterPath = TestRunner.FindTestAdapter(proj, context);

        if (testAdapterPath is null)
        {
            context.Logger.LogError("Failed to find any testadapter.");
            return false;
        }

        context.Logger.LogInformation("RunnerLocation: {}", runnerLocation);
        context.Logger.LogInformation("TestAdapter: {}", testAdapterPath);

        IVsTestConsoleWrapper consoleWrapper = new VsTestConsoleWrapper(runnerLocation);

        consoleWrapper.StartSession();
        consoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath });

        var waitHandle = new AutoResetEvent(false);
        var defaultRunSettings = "<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";

        var buildTarget = new BuildTargetIdentifier { Uri = UriFixer.WithFileSchema(proj.FullPath) };
        var discoveryHandler = new TestDiscoveryEventHandler(waitHandle, buildTarget, originId, _baseProtocolClientManager);
        consoleWrapper.DiscoverTests(targets, defaultRunSettings, discoveryHandler);

        waitHandle.WaitOne();
        consoleWrapper.EndSession();
        return true;
    }

}