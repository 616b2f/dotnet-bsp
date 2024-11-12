using BaseProtocol;
using bsp4csharp.Protocol;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint("build/initialized")]
internal class BuildInitializedHandler : INotificationHandler<InitializedBuildParams, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager;

    public BuildInitializedHandler(BuildInitializeManager initializeManager)
    {
        this._initializeManager = initializeManager;
    }

    public bool MutatesSolutionState => true;

    public Task HandleNotificationAsync(InitializedBuildParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialize();

        _initializeManager.Initialized = true;

        return Task.CompletedTask;
    }
}