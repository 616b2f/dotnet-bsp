using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;
using Microsoft.Build.Graph;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetCompile)]
internal class BuildTargetCompileHandler(
    BuildInitializeManager initializeManager,
    IBaseProtocolClientManager baseProtocolClientManager)
        : IRequestHandler<CompileParams, CompileResult, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager = initializeManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager = baseProtocolClientManager;

    public bool MutatesSolutionState => false;

    public Task<CompileResult> HandleRequestAsync(CompileParams compileParams, RequestContext context, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialized();

        var projects = new ProjectCollection();
        var buildResult = true;
        var targetFiles = compileParams.Targets.Select(x => x.ToString());
        var graph = new ProjectGraph(targetFiles, projects);
        var initParams = _initializeManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var workspacePath = initParams.RootUri.LocalPath;
            context.Logger.LogInformation("GetLoadedProjects from {}", workspacePath);
            _baseProtocolClientManager.SendClearDiagnosticsMessage();

            foreach (var proj in graph.ProjectNodesTopologicallySorted)
            {
                var globalProps = proj.ProjectInstance.GlobalProperties
                    .Select(x => string.Format("{0}={1}", x.Key, x.Value))
                    .ToArray();
                context.Logger.LogInformation("Global Properties: {}", string.Join("\n", globalProps));
                context.Logger.LogInformation("Start restore target: {}", proj.ProjectInstance.FullPath);
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, compileParams.OriginId, workspacePath);
                var result = proj.ProjectInstance.Build(["Restore"], [msBuildLogger]);
                context.Logger.LogInformation($"{proj.ProjectInstance.FullPath} restore result: {result}");
                buildResult &= result;
            }

            foreach (var proj in graph.ProjectNodesTopologicallySorted)
            {
                context.Logger.LogInformation("Start building target: {}", proj.ProjectInstance.FullPath);
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, compileParams.OriginId, workspacePath);
                var result = proj.ProjectInstance.Build(["Build"], [msBuildLogger]);
                context.Logger.LogInformation($"{proj.ProjectInstance.FullPath} build result: {result}");
                buildResult &= result;
            }
        }

        return Task.FromResult(new CompileResult
        {
            OriginId = compileParams.OriginId,
            StatusCode = buildResult ? StatusCode.Ok : StatusCode.Error
        });
    }
}