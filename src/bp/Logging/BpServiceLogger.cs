using System;
using Microsoft.Extensions.Logging;

namespace BaseProtocol;

/// <summary>
/// Implements <see cref="AbstractLspLogger"/> by sending LSP log messages back to the client.
/// </summary>
public sealed class BpServiceLogger : IBpLogger, IBpService
{
    private readonly ILogger _hostLogger;

    public BpServiceLogger(ILogger hostLogger)
    {
        _hostLogger = hostLogger;
    }

    public void LogDebug(string message, params object[] @params) => _hostLogger.LogDebug(message, @params);

    public void LogEndContext(string message, params object[] @params) => _hostLogger.LogDebug($"[{DateTime.UtcNow:hh:mm:ss.fff}][End]{message}", @params);

    public void LogError(string message, params object[] @params) => _hostLogger.LogError(message, @params);

    public void LogException(Exception exception, string? message = null, params object[] @params) => _hostLogger.LogError(exception, message, @params);

    /// <summary>
    /// TODO - Switch this to call LogInformation once appropriate callers have been changed to LogDebug.
    /// </summary>
    public void LogInformation(string message, params object[] @params) => _hostLogger.LogDebug(message, @params);

    public void LogStartContext(string message, params object[] @params) => _hostLogger.LogDebug($"[{DateTime.UtcNow:hh:mm:ss.fff}][Start]{message}", @params);

    public void LogWarning(string message, params object[] @params) => _hostLogger.LogWarning(message, @params);
}

