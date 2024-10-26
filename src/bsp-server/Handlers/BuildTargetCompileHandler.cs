using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;
using Microsoft.Build.Graph;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetCompile)]
internal class BuildTargetCompileHandler
    : IRequestHandler<CompileParams, CompileResult, RequestContext>
{
    private readonly IInitializeManager<InitializeBuildParams, InitializeBuildResult> _capabilitiesManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildTargetCompileHandler(
        IInitializeManager<InitializeBuildParams, InitializeBuildResult> capabilitiesManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _capabilitiesManager = capabilitiesManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => false;

    public Task<CompileResult> HandleRequestAsync(CompileParams compileParams, RequestContext context, CancellationToken cancellationToken)
    {
        var projects = new ProjectCollection();
        var buildResult = false;
        var targetFiles = compileParams.Targets.Select(x => x.ToString());
        var graph = new ProjectGraph(targetFiles, projects);
        var initParams = _capabilitiesManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var workspacePath = initParams.RootUri.AbsolutePath;
            context.Logger.LogInformation("GetLoadedProjects from {}", workspacePath);
            _baseProtocolClientManager.SendClearDiagnosticsMessage();

            foreach (var proj in graph.ProjectNodesTopologicallySorted)
            {
                var globalProps = proj.ProjectInstance.GlobalProperties
                    .Select(x => string.Format("{0}={1}", x.Key, x.Value))
                    .ToArray();
                context.Logger.LogInformation("Global Properties: {}", string.Join("\n", globalProps));
                context.Logger.LogInformation("Start restore target: {}", proj.ProjectInstance.FullPath);
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, compileParams.OriginId, workspacePath, proj.ProjectInstance.FullPath);
                buildResult |= proj.ProjectInstance.Build(["Restore"], new [] {msBuildLogger});
            }

            foreach (var proj in graph.ProjectNodesTopologicallySorted)
            {
                context.Logger.LogInformation("Start building target: {}", proj.ProjectInstance.FullPath);
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, compileParams.OriginId, workspacePath, proj.ProjectInstance.FullPath);
                buildResult |= proj.ProjectInstance.Build(["Build"], new [] {msBuildLogger});
            }
        }

        return Task.FromResult(new CompileResult
        {
            OriginId = compileParams.OriginId,
            StatusCode = buildResult ? StatusCode.Ok : StatusCode.Error
        });
    }
}