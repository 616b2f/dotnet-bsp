using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using dotnet_bsp.Logging;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetCleanCache)]
internal class BuildTargetCleanCacheHandler(
    BuildInitializeManager initializeManager,
    IBaseProtocolClientManager baseProtocolClientManager)
        : IRequestHandler<CleanCacheParams, CleanCacheResult, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager = initializeManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager = baseProtocolClientManager;

    public bool MutatesSolutionState => false;

    public Task<CleanCacheResult> HandleRequestAsync(CleanCacheParams cleanCacheParams, RequestContext context, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialized();

        var projects = new ProjectCollection();
        var cleanResult = false;
        var targetFiles = BuildHelper.ExtractProjectsFromSolutions(cleanCacheParams.Targets);
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
                        new MSBuildLogger(_baseProtocolClientManager, null, workspacePath)
                    ],
                },
                []);

            context.Logger.LogInformation("Start clean for target: {}", targetFiles);
            var graphCleanResult = buildManager.BuildRequest(new GraphBuildRequestData(graph, ["Clean"]));
            context.Logger.LogInformation("Clean result: {}", graphCleanResult.OverallResult);
            cleanResult = (graphCleanResult.OverallResult == BuildResultCode.Success);
            buildManager.EndBuild();
        }

        var message = cleanResult ? "Cleaned" : "Not cleaned";

        return Task.FromResult(new CleanCacheResult
        {
            Message = message,
            Cleaned = cleanResult
        });
    }
}