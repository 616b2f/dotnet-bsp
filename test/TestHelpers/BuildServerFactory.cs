using dotnet_bsp;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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

public sealed class TestBuildServer : IDisposable
{
    private readonly Stream _serverStdin;
    private readonly Stream _serverStdout;
    private readonly Process _process;
    private readonly ILogger _logger;

    public TestBuildServer(ILogger logger)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo(
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
                // RedirectStandardError = true,
            }
        };

        _process.Start();

        _serverStdin = _process.StandardInput.BaseStream;
        _serverStdout = _process.StandardOutput.BaseStream;
        _process.ErrorDataReceived += ErrorDataReceived;
        _logger = logger;
    }

    private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        _logger.LogError(e.Data);
    }

    public BuildServerClient CreateClient(ServerCallbacks serverCallbacks, TraceListener? traceListener = null)
    {
        return new BuildServerClient(_serverStdin, _serverStdout, serverCallbacks, traceListener);
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        return _process.WaitForExitAsync(cancellationToken);
    }

    public int ExitCode => _process.ExitCode;

    public void Dispose()
    {
        // var errors = _process.StandardError.ReadToEnd();
        // _logger.LogError(errors);
        if (!_process.HasExited)
        {
            _process.Kill(ProcessExtensions.SIGINT);
        }

        _process.Dispose();
    }
}