using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace bsp_client;

public class BuildServerFactory
{
    private BuildServerFactory()
    {
    }

    public static BuildServer CreateServer(BspConnectionDetails connectionDetails, ILogger logger)
    {
        return new BuildServer(connectionDetails, logger);
    }
}

public sealed class BuildServer : IDisposable
{
    private readonly Stream _serverStdin;
    private readonly Stream _serverStdout;
    private readonly Process _process;
    private readonly ILogger _logger;

    public BuildServer(BspConnectionDetails connectionDetails, ILogger logger)
    {
        var command = connectionDetails.Argv[0];
        var args = connectionDetails.Argv[1..];
        _process = new Process
        {
            StartInfo = new ProcessStartInfo(command, args)
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

    public BuildServerClient CreateClient(
        IServerCallbacks serverCallbacks,
        TraceListener? traceListener = null)
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _process.Kill();
            }
            else
            {
                _process.Kill(ProcessExtensions.SIGINT);
            }
        }

        _process.Dispose();
    }
}