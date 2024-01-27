using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

internal class BspLogMessageLoggerProvider : ILoggerProvider
{
    private readonly ILoggerFactory _fallbackLoggerFactory;
    private readonly ConcurrentDictionary<string, BspLogMessageLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public BspLogMessageLoggerProvider(ILoggerFactory fallbackLoggerFactory)
    {
        _fallbackLoggerFactory = fallbackLoggerFactory;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, new BspLogMessageLogger(categoryName, _fallbackLoggerFactory));
    }

    public void Dispose()
    {
        _loggers.Clear();
        _fallbackLoggerFactory.Dispose();
    }
}
