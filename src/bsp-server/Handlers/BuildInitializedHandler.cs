using BaseProtocol;
using bsp4csharp.Protocol;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint("build/initialized")]
internal class BuildInitializedHandler(BuildInitializeManager initializeManager) : INotificationHandler<InitializedBuildParams, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager = initializeManager;

    public bool MutatesSolutionState => true;

    public Task HandleNotificationAsync(InitializedBuildParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialize();

        _initializeManager.Initialized = true;

        return Task.CompletedTask;
    }
}