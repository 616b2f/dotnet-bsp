using dotnet_bsp;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;
using StreamJsonRpc;
using System.Diagnostics;
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
    private Stream serverStdin;
    private Stream serverStdout;
    private readonly Process _process;

    public TestBuildServer(ILogger logger)
    {
        _process = new Process();
        _process.StartInfo = new ProcessStartInfo(
            "dotnet",
            [
                "exec",
                Path.Combine(AppContext.BaseDirectory, "dotnet-bsp.dll"),
                "--logLevel=Debug",
                "--extensionLogDirectory",
                "."
            ])
            {
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };

        _process.Start();

        serverStdin = _process.StandardInput.BaseStream;
        serverStdout = _process.StandardOutput.BaseStream;
    }

    public BuildServerClient CreateClient()
    {
        return new BuildServerClient(serverStdin, serverStdout);
    }

    public Task WaitForExitAsync()
    {
        return _process.WaitForExitAsync();
    }

    ~TestBuildServer()
    {
        if (!_process.HasExited)
        {
            _process.Kill(ProcessExtensions.SIGINT);
        }

        _process.Dispose();
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