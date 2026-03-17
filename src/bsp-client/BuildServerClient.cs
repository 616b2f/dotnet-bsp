using StreamJsonRpc;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace bsp_client;

public sealed class BuildServerClient : IDisposable
{
    private readonly JsonRpc _jsonRpc;

    public BuildServerClient(Stream sendingStream, Stream receivingStream, IServerCallbacks serverCallbacks, TraceListener? traceListener = null)
    {
        _jsonRpc = new JsonRpc(sendingStream, receivingStream);
        _jsonRpc.AddLocalRpcTarget(serverCallbacks);
        if (traceListener is not null)
        {
            _jsonRpc.TraceSource.Listeners.Add(traceListener);
        }
        _jsonRpc.StartListening();
    }

    public async Task SendRequestAsync(string methodName)
        => await _jsonRpc.InvokeAsync(methodName).ConfigureAwait(false);

    public Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams requestParameters, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithParameterObjectAsync<TResponse>(methodName, requestParameters, cancellationToken);

    public Task<TResponse> SendRequestAsync<TResponse>(string methodName, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithParameterObjectAsync<TResponse>(methodName, cancellationToken: cancellationToken);

    public Task SendNotificationAsync<TParams>(string methodName, TParams notificationParams)
        => _jsonRpc.NotifyAsync(methodName, notificationParams);

    public async Task SendNotificationAsync(string methodName)
        => await _jsonRpc.NotifyAsync(methodName).ConfigureAwait(false);

    public void Dispose()
    {
        _jsonRpc.Dispose();
    }
}