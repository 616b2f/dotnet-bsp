using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using dotnet_bsp.EventHandlers;
using Microsoft.Build.Graph;
using Microsoft.Build.Execution;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetTestCaseDiscovery)]
internal partial class BuildTargetTestCaseDiscoveryHandler
    : IRequestHandler<TestCaseDiscoveryParams, TestCaseDiscoveryResult, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildTargetTestCaseDiscoveryHandler(
        BuildInitializeManager initializeManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _initializeManager = initializeManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => false;

    public Task<TestCaseDiscoveryResult> HandleRequestAsync(TestCaseDiscoveryParams testCaseDiscoveryParams, RequestContext context, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialized();

        var projects = new ProjectCollection();
        var buildResult = true;
        var testCaseDiscoveryResult = true;
        var targetFiles = testCaseDiscoveryParams.Targets.Select(x => x.ToString());
        var initParams = _initializeManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var workspacePath = initParams.RootUri.LocalPath;
            context.Logger.LogInformation("GetLoadedProjects from {}", workspacePath);
            _baseProtocolClientManager.SendClearDiagnosticsMessage();

            var graph = new ProjectGraph(targetFiles, projects);
            foreach (var proj in graph.ProjectNodesTopologicallySorted)
            {
                var globalProps = proj.ProjectInstance.GlobalProperties
                    .Select(x => string.Format("{0}={1}", x.Key, x.Value))
                    .ToArray();
                context.Logger.LogInformation("Global Properties: {}", string.Join("\n", globalProps));
                context.Logger.LogInformation("Start restore target: {}", proj.ProjectInstance.FullPath);
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, testCaseDiscoveryParams.OriginId, workspacePath, proj.ProjectInstance.FullPath);
                var result = proj.ProjectInstance.Build(["Restore"], [msBuildLogger]);
                context.Logger.LogInformation($"{proj.ProjectInstance.FullPath} restore result: {result}");
                buildResult &= result;
            }

            if (buildResult)
            {
                graph = new ProjectGraph(targetFiles, projects);
                foreach (var projNode in graph.ProjectNodesTopologicallySorted)
                {
                    context.Logger.LogInformation("Start building target: {}", projNode.ProjectInstance.FullPath);
                    var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, testCaseDiscoveryParams.OriginId, workspacePath, projNode.ProjectInstance.FullPath);
                    var result = projNode.ProjectInstance.Build(["Build"], [msBuildLogger]);
                    context.Logger.LogInformation($"{projNode.ProjectInstance.FullPath} build result: {result}");
                    buildResult &= result;
                }
            }

            if (buildResult)
            {
                graph = new ProjectGraph(targetFiles, projects);
                foreach (var proj in graph.ProjectNodesTopologicallySorted)
                {
                    context.Logger.LogInformation("Start test target: {}", proj.ProjectInstance.FullPath);
                    var targetPath = proj.ProjectInstance.Properties.First(x => x.Name == "TargetPath").EvaluatedValue;
                    context.Logger.LogInformation("targetPath: {}", targetPath);

                    var result = RunTestDiscovery(testCaseDiscoveryParams.OriginId, proj.ProjectInstance, [targetPath], context);

                    if (!result)
                    {
                        testCaseDiscoveryResult = result;
                    }
                }
            }
        }

        return Task.FromResult(new TestCaseDiscoveryResult
        {
            OriginId = testCaseDiscoveryParams.OriginId,
            StatusCode = testCaseDiscoveryResult ? StatusCode.Ok : StatusCode.Error
        });
    }

    private bool RunTestDiscovery(string? originId, ProjectInstance proj, IEnumerable<string> targets, RequestContext context)
    {
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

        var targetPath = proj.Properties.First(x => x.Name == "TargetPath").EvaluatedValue;
        var testAdapterPath = TestRunner.FindTestAdapter(targetPath, context);

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