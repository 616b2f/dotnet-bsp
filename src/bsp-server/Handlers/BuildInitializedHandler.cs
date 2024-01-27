using BaseProtocol;
using bsp4csharp.Protocol;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint("build/initialized")]
internal class BuildInitializedHandler : INotificationHandler<InitializedBuildParams, RequestContext>
{
    private bool HasBeenInitialized = false;

    public bool MutatesSolutionState => true;

    public Task HandleNotificationAsync(InitializedBuildParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        if (HasBeenInitialized)
        {
            throw new InvalidOperationException("initialized was called twice");
        }

        HasBeenInitialized = true;

        return Task.CompletedTask;
    }
}
