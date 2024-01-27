using Microsoft.Extensions.Logging;

namespace dotnet_bsp;

internal record class ServerConfiguration(
    bool LaunchDebugger,
    LogLevel MinimumLogLevel,
    string ExtensionLogDirectory);
