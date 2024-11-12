using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;
using Microsoft.Build.Execution;
using System.Diagnostics;
using Newtonsoft.Json;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetRun)]
internal class BuildTargetRunHandler
    : IRequestHandler<RunParams, RunResult, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildTargetRunHandler(
        BuildInitializeManager initializeManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _initializeManager = initializeManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => false;

    public async Task<RunResult> HandleRequestAsync(RunParams runParams, RequestContext context, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialized();

        var runResult = true;
        var initParams = _initializeManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var projects = new ProjectCollection();
            var target = runParams.Target.ToString();
            var fileExtension = Path.GetExtension(target);
            if (TryGetLaunchProfile(target, out string launchProfileName, out string launchSettingsFile))
            {
                context.Logger.LogInformation("Run launchProfile: " + launchProfileName);
                context.Logger.LogInformation("From LaunchProfileSettings file: " + target);

                var directory = Path.GetDirectoryName(launchSettingsFile)
                    ?? throw new DirectoryNotFoundException($"Directory not found for target: {target}");
                var projectRootDir = Directory.GetParent(directory)
                    ?? throw new DirectoryNotFoundException("Project root path can not be root directory!");
                var projectFile = projectRootDir.GetFiles().Where(x => x.Extension == ".csproj").FirstOrDefault()
                    ?? throw new FileNotFoundException($"csproj file not found in '{projectRootDir}'");

                if (LaunchSettings.TryLoadLaunchSettings(launchSettingsFile, out LaunchSettings? launchSettings))
                {
                    var launchProfile = launchSettings!.Profiles
                        .First(x => x.Key.Equals(launchProfileName, StringComparison.InvariantCultureIgnoreCase)).Value;

                    context.Logger.LogInformation($"LaunchProfile: {JsonConvert.SerializeObject(launchProfile)}");
                    var commandLineArgs = launchProfile.CommandLineArgs?.Split(" ", StringSplitOptions.TrimEntries) ?? [];
                    await RunTargetAsync(projectFile.FullName, runParams.OriginId, context, commandLineArgs, launchProfile.EnvironmentVariables, cancellationToken);
                }
            }
            else if (fileExtension == ".csproj")
            {
                var commandLineArgs = runParams.Arguments ?? [];
                await RunTargetAsync(target, runParams.OriginId, context, commandLineArgs, runParams.EnvironmentVariables, cancellationToken);
            }
        }

        return new RunResult
        {
            OriginId = runParams.OriginId,
            StatusCode = runResult ? StatusCode.Ok : StatusCode.Error
        };
    }

    private async Task RunTargetAsync(string target, string? originId, RequestContext context, string[] commandLineArgs, Dictionary<string, string> environmentVariables, CancellationToken cancellationToken)
    {
        var projectFile = target;
        var proj = new ProjectInstance(projectFile);
        var initParams = _initializeManager.GetInitializeParams();
        var workspacePath = initParams.RootUri.AbsolutePath;

        var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, originId, workspacePath, projectFile);
        proj.Build(["Restore", "Build"], [msBuildLogger]);

        var command = proj.GetPropertyValue("RunCommand");

        await Task.Run(async () => {
            using Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = workspacePath;

            foreach (var commandLineArg in commandLineArgs)
            {
                process.StartInfo.ArgumentList.Add(commandLineArg);
            }

            foreach (var envVar in environmentVariables)
            {
                process.StartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
            }

            var fullCommand = string.Format("{0} {1}", command, string.Join(" ", commandLineArgs));
            context.Logger.LogInformation("Command to run: " + fullCommand);

            void HandleProcessExit(object? sender, EventArgs e)
            {
                if (!process.WaitForExit(0))
                {
                    process.Kill(ProcessExtensions.SIGINT);
                }
            }

            void HandleCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
            {
                // Ignore SIGINT/SIGQUIT so that the process can handle the signal
                e.Cancel = true;
            }

            Console.CancelKeyPress += HandleCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;

            cancellationToken.Register(() => process.Kill(ProcessExtensions.SIGINT));

            process.Start();

            // var stdin = process.StandardInput;

            var stdout = process.StandardOutput;
            var stdoutTask = Task.Run(async () => {
                while (await stdout.ReadLineAsync() is string line && line != null)
                {
                    var printParams = new PrintParams { Message = line };
                    await _baseProtocolClientManager.SendNotificationAsync<PrintParams>(
                        Methods.RunPrintStdout,
                        printParams, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            });

            var stderr = process.StandardError;
            _ = Task.Run(async () => {
                while (await stderr.ReadLineAsync() is string line && line != null)
                {
                    var printParams = new PrintParams { Message = line };
                    await _baseProtocolClientManager.SendNotificationAsync<PrintParams>(
                        Methods.RunPrintStderr,
                        printParams, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            });

            await process.WaitForExitAsync(cancellationToken);
        },
        cancellationToken);
    }

    private bool TryGetLaunchProfile(string fileExtension, out string launchProfile, out string launchSettingsFile)
    {
        launchSettingsFile = string.Empty;
        launchProfile = string.Empty;

        var fileParts = fileExtension.Split("#");
        if (fileParts.Count() == 2)
        {
            launchSettingsFile = fileParts[0];
            launchProfile = fileParts[1];
            return true;
        }

        return false;
    }
}