using Microsoft.Extensions.Logging;
using Nerdbank.Streams;
using StreamJsonRpc;
using BspMethods = bsp4csharp.Protocol.Methods;

namespace test;

public class BuildServerFactory
{
    private BuildServerFactory()
    {
    }

    public static TestBuildServer CreateServer(ILogger logger)
    {
        return new TestBuildServer(logger);
    }
}

public class TestBuildServer
{
    private BuildServerHost server;
    private Stream serverStdin;
    private Stream serverStdout;

    public TestBuildServer(ILogger logger)
    {
        var (serverInputStream, clientOutputStream) = FullDuplexStream.CreatePair();
        var (clientInputStream, serverOutputStream) = FullDuplexStream.CreatePair();

        serverStdin = clientOutputStream;
        serverStdout = clientInputStream;

        server = new BuildServerHost(serverInputStream, serverOutputStream, logger);
        server.Start();
    }

    public BuildServerClient CreateClient()
    {
        return new BuildServerClient(serverStdin, serverStdout);
    }

    public Task WaitForExitAsync()
    {
        return server.WaitForExitAsync();
    }
}

public class BuildServerClient
{
    private JsonRpc _jsonRpc;

    public BuildServerClient(Stream sendingStream, Stream receivingStream)
    {
        _jsonRpc = JsonRpc.Attach(sendingStream, receivingStream);
    }

    public Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams requestParameters, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithParameterObjectAsync<TResponse>(methodName, requestParameters, cancellationToken);

    public Task<TResponse> SendRequestAsync<TResponse>(string methodName, CancellationToken cancellationToken)
        => _jsonRpc.InvokeWithParameterObjectAsync<TResponse>(methodName, cancellationToken: cancellationToken);
        // => _jsonRpc.InvokeWithCancellationAsync<TResponse>(methodName, cancellationToken: cancellationToken);

    public Task SendNotificationAsync<TParams>(string methodName, TParams notificationParams)
        => _jsonRpc.NotifyAsync(methodName, notificationParams);

    // public const string BuildShowMessage = "build/showMessage";
    // public const string BuildLogMessage = "build/logMessage";
    // public const string BuildPublishDiagnostics = "build/publishDiagnostics";
    // public const string BuildTargetDidChange = "buildTarget/didChange";
    // public const string BuildTaskStart = "build/taskStart";
    // public const string BuildTaskProgress = "build/taskProgress";
    // public const string BuildTaskFinish = "build/taskFinish";
    // public const string RunPrintStdout = "run/printStdout";
    // public const string RunPrintStderr = "run/printStderr";
    [JsonRpcMethod(BspMethods.BuildPublishDiagnostics)]
    public Task BuildPublishDiagnosticsRecievedAsync()
    {
        return Task.CompletedTask;
    }
}