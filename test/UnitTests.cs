using bsp4csharp.Protocol;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace test;

public class UnitTests
{
    private readonly ITestOutputHelper outputHelper;

    public UnitTests(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
    }

    [Fact]
    public async Task InitializeBuildRequest_Success()
    {
        // // Arrange
        var testlogger = new UnitTestLogger(outputHelper);
        var buildServer = BuildServerFactory.CreateServer(testlogger);
        var client = buildServer.CreateClient();

        var cancelationTokenSource = new CancellationTokenSource();
        var initParams = new InitializeBuildParams
        {
            DisplayName = "TestClient",
            Version = "1.0.0",
            BspVersion = "2.1.1",
            RootUri = new Uri("/home/ak/devel/aspnet-example/"),
            Capabilities = new BuildClientCapabilities()
        };
        initParams.Capabilities.LanguageIds.Add("csharp");

        // Act
        var initResult = await client.SendRequestAsync<InitializeBuildParams, InitializeBuildResult>(Methods.BuildInitialize, initParams, cancelationTokenSource.Token);

        // Assert
        Assert.Equal("dotnet-bsp", initResult.DisplayName);
    }

    [Fact]
    public void Test2()
    {
        Assert.True(true);
    }
}

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