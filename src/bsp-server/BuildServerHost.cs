using BaseProtocol;
using BaseProtocol.Server;
using dotnet_bsp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using System.Runtime.CompilerServices;

#pragma warning disable CA1001 // The JsonRpc instance is disposed of by the AbstractLanguageServer during shutdown
[assembly: InternalsVisibleTo("bsp-server.tests")]
internal sealed class BuildServerHost
#pragma warning restore CA1001 // The JsonRpc instance is disposed of by the AbstractLanguageServer during shutdown
{
    /// <summary>
    /// A static reference to the server instance.
    /// Used by components to send notifications and requests back to the client.
    /// </summary>
    internal static BuildServerHost? Instance { get; private set; }

    private readonly ILogger _logger;
    private readonly AbstractBaseProtocolServer<RequestContext> _buildServer;
    private readonly JsonRpc _jsonRpc;

    public BuildServerHost(Stream inputStream, Stream outputStream, ILogger logger)
    {
        var handler = new HeaderDelimitedMessageHandler(outputStream, inputStream, new JsonMessageFormatter());

        // If there is a jsonrpc disconnect or server shutdown, that is handled by the AbstractLanguageServer.  No need to do anything here.
        _jsonRpc = new JsonRpc(handler)
        {
            ExceptionStrategy = ExceptionProcessing.CommonErrorData,
        };

        _logger = logger;
        var bpLogger = new BpServiceLogger(_logger);

        Action<IServiceCollection>? hostServices = null;

        _buildServer = new BuildServer(
            _jsonRpc,
            bpLogger,
            hostServices);
    }

    public void Start()
    {
        _jsonRpc.StartListening();

        using (var logScope = _logger.BeginScope<BuildServerHost>(this))
        {
            _logger.LogInformation("BuildServerHost started");
        }

        // Now that the server is started, update the our instance reference
        Instance = this;
    }

    public async Task WaitForExitAsync()
    {
        await _jsonRpc.Completion;
        await _buildServer.WaitForExitAsync();
    }

    public T GetRequiredBspService<T>() where T : IBpService
    {
        return _buildServer.GetBpServices().GetRequiredService<T>();
    }
}