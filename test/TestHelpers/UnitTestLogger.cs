using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace test;

public class UnitTestLogger: ILogger
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly LogLevel _logLevel;

    public UnitTestLogger(ITestOutputHelper outputHelper, LogLevel logLevel = LogLevel.Warning)
    {
        _outputHelper = outputHelper;
        _logLevel = logLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (_logLevel >= logLevel)
        {
            _outputHelper.WriteLine(string.Format("LogLevel: {0}, LogMessage: {1}", logLevel, formatter(state, exception)));
        }
    }
}