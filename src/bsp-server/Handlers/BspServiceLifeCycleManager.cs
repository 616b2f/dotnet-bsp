using BaseProtocol;
using bsp4csharp.Protocol;
using StreamJsonRpc;

namespace dotnet_bsp.Handlers;

public class BspServiceLifeCycleManager(IBaseProtocolClientManager baseProtocolClientManager) : ILifeCycleManager, IBpService
{
    private readonly IBaseProtocolClientManager _baseProtocolClientManager = baseProtocolClientManager;

    public async Task ShutdownAsync(string message = "Shutting down")
    {
        try
        {
            var messageParams = new LogMessageParams()
            {
                MessageType = MessageType.Info,
                Message = message
            };
            await _baseProtocolClientManager.SendNotificationAsync(Methods.BuildLogMessage, messageParams, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
        {
            //Don't fail shutdown just because jsonrpc has already been cancelled.
        }
    }

    public Task ExitAsync()
    {
        // We don't need any custom logic to run on exit.
        return Task.CompletedTask;
    }
}
