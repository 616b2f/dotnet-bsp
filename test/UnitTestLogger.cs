using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace test;

public class UnitTestLogger: ILogger
{
    private readonly ITestOutputHelper outputHelper;

    public UnitTestLogger(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
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
        outputHelper.WriteLine(string.Format("LogLevel: {0}, LogMessage: {1}", logLevel, formatter(state, exception)));
    }
}