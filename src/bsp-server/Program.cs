using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using dotnet_bsp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

// WindowsErrorReporting.SetErrorModeOnWindows();

var parser = CreateCommandLineParser();
return await parser.Parse(args).InvokeAsync(CancellationToken.None);

static async Task<int> RunAsync(ServerConfiguration serverConfiguration, CancellationToken cancellationToken)
{
    // Before we initialize the LSP server we can't send LSP log messages.
    // Create a console logger as a fallback to use before the LSP server starts.
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .SetMinimumLevel(serverConfiguration.MinimumLogLevel)
            .AddProvider(new BspLogMessageLoggerProvider(fallbackLoggerFactory:
                // Add a console logger as a fallback for when the LSP server has not finished initializing.
                LoggerFactory.Create(builder =>
                {
                    builder.SetMinimumLevel(serverConfiguration.MinimumLogLevel);
                    builder.AddConsole();
                    builder.AddSimpleConsole(formatterOptions => formatterOptions.ColorBehavior = LoggerColorBehavior.Disabled);
                })
            ));
    });

    var logger = loggerFactory.CreateLogger<Program>();

    if (serverConfiguration.LaunchDebugger)
    {
        var timeout = TimeSpan.FromMinutes(1);
        logger.LogCritical($"Server started with process ID {Environment.ProcessId}");
        logger.LogCritical($"Waiting {timeout:g} for a debugger to attach");
        using var timeoutSource = new CancellationTokenSource(timeout);
        while (!Debugger.IsAttached && !timeoutSource.Token.IsCancellationRequested)
        {
            await Task.Delay(100, CancellationToken.None);
        }
    }

    logger.LogTrace($".NET Runtime Version: {RuntimeInformation.FrameworkDescription}");

    // The log file directory passed to us by VSCode might not exist yet, though its parent directory is guaranteed to exist.
    if (!string.IsNullOrWhiteSpace(serverConfiguration.ExtensionLogDirectory))
    {
        Directory.CreateDirectory(serverConfiguration.ExtensionLogDirectory);
    }

    var buildServerLogger = loggerFactory.CreateLogger(nameof(BuildServerHost));

    // var (clientPipeName, serverPipeName) = CreateNewPipeNames();
    // var pipeServer = new NamedPipeServerStream(serverPipeName,
    //     PipeDirection.InOut,
    //     maxNumberOfServerInstances: 1,
    //     PipeTransmissionMode.Byte,
    //     PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);
    //
    // // Send the named pipe connection info to the client
    // Console.WriteLine(JsonConvert.SerializeObject(new NamedPipeInformation(clientPipeName)));
    //
    // // Wait for connection from client
    // await pipeServer.WaitForConnectionAsync(cancellationToken);

    // var server = new BuildServerHost(pipeServer, pipeServer, buildServerLogger);
    var stdin = Console.OpenStandardInput();
    var stdout = Console.OpenStandardOutput();
    var server = new BuildServerHost(stdin, stdout, buildServerLogger);

    server.Start();

    try
    {
        await server.WaitForExitAsync().ConfigureAwait(false);
        return 0;
    }
    catch (Exception e)
    {
        logger.LogError(e, "Server exit with an exception");
        return 1;
    }
}

static CliRootCommand CreateCommandLineParser()
{
    var debugOption = new CliOption<bool>("--debug")
    {
        Description = "Flag indicating if the debugger should be launched on startup.",
        Required = false,
        DefaultValueFactory = _ => false,
    };
    var brokeredServicePipeNameOption = new CliOption<string?>("--brokeredServicePipeName")
    {
        Description = "The name of the pipe used to connect to a remote process (if one exists).",
        Required = false,
    };

    var logLevelOption = new CliOption<LogLevel>("--logLevel")
    {
        Description = "The minimum log verbosity.",
        Required = true,
    };

    var extensionLogDirectoryOption = new CliOption<string>("--extensionLogDirectory")
    {
        Description = "The directory where we should write log files to",
        Required = true,
    };

    var rootCommand = new CliRootCommand()
    {
        debugOption,
        brokeredServicePipeNameOption,
        logLevelOption,
        extensionLogDirectoryOption
    };
    rootCommand.SetAction((parseResult, cancellationToken) =>
    {
        var launchDebugger = parseResult.GetValue(debugOption);
        var logLevel = parseResult.GetValue(logLevelOption);
        var extensionLogDirectory = parseResult.GetValue(extensionLogDirectoryOption)!;

        var serverConfiguration = new ServerConfiguration(
            LaunchDebugger: launchDebugger,
            MinimumLogLevel: logLevel,
            ExtensionLogDirectory: extensionLogDirectory);

        return RunAsync(serverConfiguration, cancellationToken);
    });
    return rootCommand;
}

static (string clientPipe, string serverPipe) CreateNewPipeNames()
{
    // On windows, .NET and Nodejs use different formats for the pipe name
    const string WINDOWS_NODJS_PREFIX = @"\\.\pipe\";
    const string WINDOWS_DOTNET_PREFIX = @"\\.\";

    // The pipe name constructed by some systems is very long (due to temp path).
    // Shorten the unique id for the pipe. 
    var newGuid = Guid.NewGuid().ToString();
    var pipeName = newGuid.Split('-')[0];

    return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? (WINDOWS_NODJS_PREFIX + pipeName, WINDOWS_DOTNET_PREFIX + pipeName)
        : (GetUnixTypePipeName(pipeName), GetUnixTypePipeName(pipeName));

    static string GetUnixTypePipeName(string pipeName)
    {
        // Unix-type pipes are actually writing to a file
        return Path.Combine(Path.GetTempPath(), pipeName + ".sock");
    }
}