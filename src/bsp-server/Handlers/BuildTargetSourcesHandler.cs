using BaseProtocol;
using bsp4csharp.Protocol;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetSources)]
internal class BuildTargetSourcesHandler
    : IRequestHandler<SourcesParams, SourcesResult, RequestContext>
{
    private readonly IInitializeManager<InitializeBuildParams, InitializeBuildResult> _capabilitiesManager;

    public BuildTargetSourcesHandler(IInitializeManager<InitializeBuildParams, InitializeBuildResult> capabilitiesManager)
    {
        _capabilitiesManager = capabilitiesManager;
    }

    public bool MutatesSolutionState => false;

    public Task<SourcesResult> HandleRequestAsync(SourcesParams sourcesParams, RequestContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SourcesResult());
    }
}

