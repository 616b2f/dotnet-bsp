using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using dotnet_bsp.Logging;

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
        var targetFiles = BuildHelper.ExtractProjectsFromSolutions(compileParams.Targets);
        var graph = new ProjectGraph(targetFiles, projects);
        var initParams = _initializeManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var workspacePath = initParams.RootUri.LocalPath;
            context.Logger.LogInformation("GetLoadedProjects from {}", workspacePath);
            _baseProtocolClientManager.SendClearDiagnosticsMessage();

            var buildManager = _initializeManager.GetBuildManager();
            buildManager.BeginBuild(new BuildParameters
                {
                    Loggers = [
                        new MSBuildLogger(_baseProtocolClientManager, compileParams.OriginId, workspacePath)
                    ],
                },
                []);

            context.Logger.LogInformation("Start restore targets: {}", targetFiles);
            var graphRestoreResult = buildManager.BuildRequest(new GraphBuildRequestData(graph, ["Restore"]));
            context.Logger.LogInformation("Restore result: {}", graphRestoreResult.OverallResult);
            buildResult &= (graphRestoreResult.OverallResult == BuildResultCode.Success);
            context.Logger.LogInformation("Start building targets: {}", targetFiles);
            var graphBuildResult = buildManager.BuildRequest(new GraphBuildRequestData(graph, ["Build"]));
            context.Logger.LogInformation("Build result: {}", graphBuildResult.OverallResult);
            buildResult &= (graphBuildResult.OverallResult == BuildResultCode.Success);
            buildManager.EndBuild();
        }

        return Task.FromResult(new CompileResult
        {
            OriginId = compileParams.OriginId,
            StatusCode = buildResult ? StatusCode.Ok : StatusCode.Error
        });
    }
}