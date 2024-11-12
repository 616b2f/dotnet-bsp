using System;
using System.Threading;
using System.Threading.Tasks;

namespace BaseProtocol.Handlers;

[BaseProtocolServerEndpoint("initialized")]
public class InitializedHandler<TRequest, TRequestContext> : INotificationHandler<TRequest, TRequestContext>
{
    private bool HasBeenInitialized = false;

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => true;

    public Task HandleNotificationAsync(TRequest request, TRequestContext requestContext, CancellationToken cancellationToken)
    {
        if (HasBeenInitialized)
        {
            throw new InvalidOperationException("initialized was called twice");
        }

        HasBeenInitialized = true;

        return Task.CompletedTask;
    }
}