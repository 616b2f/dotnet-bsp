using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;
using Microsoft.Build.Execution;
using System.Diagnostics;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetRun)]
internal class BuildTargetRunHandler
    : IRequestHandler<RunParams, RunResult, RequestContext>
{
    private readonly IInitializeManager<InitializeBuildParams, InitializeBuildResult> _capabilitiesManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildTargetRunHandler(
        IInitializeManager<InitializeBuildParams, InitializeBuildResult> capabilitiesManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _capabilitiesManager = capabilitiesManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => false;

    public async Task<RunResult> HandleRequestAsync(RunParams runParams, RequestContext context, CancellationToken cancellationToken)
    {
        var runResult = true;
        var initParams = _capabilitiesManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var projects = new ProjectCollection();
            var target = runParams.Target;
            var fileExtension = Path.GetExtension(target.Uri.ToString());
            if (fileExtension == ".csproj")
            {
                var projectFile = target.Uri.ToString();
                var proj = new ProjectInstance(projectFile);
                var workspacePath = initParams.RootUri.AbsolutePath;

                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, workspacePath, projectFile);
                proj.Build(["Restore", "Build"], [msBuildLogger]);

                var command = proj.GetPropertyValue("RunCommand");

                await Task.Run(async () => {
                    using Process process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = command;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.WorkingDirectory = workspacePath;

                    foreach (var envVar in runParams.EnvironmentVariables)
                    {
                        process.StartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                    }

                    context.Logger.LogInformation("Command to run: " + command);


                    void HandleProcessExit(object? sender, EventArgs e)
                    {
                        if (!process.WaitForExit(0))
                        {
                            process.Kill();
                        }
                    }

                    void HandleCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
                    {
                        // Ignore SIGINT/SIGQUIT so that the process can handle the signal
                        e.Cancel = true;
                    }

                    Console.CancelKeyPress += HandleCancelKeyPress;
                    AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;

                    process.Start();

                    // var stdin = process.StandardInput;

                    var stdout = process.StandardOutput;
                    var stdoutTask = Task.Run(async () => {
                        while (await stdout.ReadLineAsync() is string line && line != null)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            var printParams = new PrintParams { Message = line };
                            await _baseProtocolClientManager.SendNotificationAsync<PrintParams>(
                                Methods.RunPrintStdout,
                                printParams, cancellationToken);
                        }
                    });

                    var stderr = process.StandardError;
                    _ = Task.Run(async () => {
                        while (await stderr.ReadLineAsync() is string line && line != null)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            var printParams = new PrintParams { Message = line };
                            await _baseProtocolClientManager.SendNotificationAsync<PrintParams>(
                                Methods.RunPrintStderr,
                                printParams, cancellationToken);
                        }
                    });


                    var periodicTimer= new PeriodicTimer(TimeSpan.FromSeconds(3));
                    while (await periodicTimer.WaitForNextTickAsync())
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            context.Logger.LogInformation("Cancelation requested, kill process");
                            process.Kill();
                            break;
                        }
                    }

                    process.WaitForExit();
                    context.Logger.LogInformation("WaitForExit returned");
                });
            }
        }

        return new RunResult
        {
            StatusCode = runResult ? StatusCode.Ok : StatusCode.Error
        };
    }
}