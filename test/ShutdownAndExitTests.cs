using Xunit.Abstractions;

namespace test;

public partial class ShutdownAndExitTets(ITestOutputHelper outputHelper)
{
    private readonly UnitTestLogger _testlogger = new(outputHelper);

    [Fact]
    public async Task RequestToExit_WithShutdownRequestedBefore_SuccessWithExitCodeZero()
    {
        // Arrange
        using var buildServer = BuildServerFactory.CreateServer(_testlogger);
        var serverCallbacks = new ServerCallbacks(outputHelper);
        var client = buildServer.CreateClient(serverCallbacks, new XunitTraceListener(outputHelper));

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(3));
        var cancellationToken = cancellationTokenSource.Token;

        _ = await client.BuildInitializeAsync(TestProjectPath.AspnetWithoutErrors, cancellationToken);
        await client.BuildInitializedAsync();

        await client.ShutdownAsync();

        // Act
        await client.ExitAsync();
        await buildServer.WaitForExitAsync(cancellationToken);

        // Assert
        Assert.Equal(0, buildServer.ExitCode);
    }

    [Fact]
    public async Task RequestToExit_WithoutShutdownFirst_FailWithExitCodeOne()
    {
        // Arrange
        var testlogger = new UnitTestLogger(outputHelper);
        using var buildServer = BuildServerFactory.CreateServer(testlogger);
        var serverCallbacks = new ServerCallbacks(outputHelper);
        var client = buildServer.CreateClient(serverCallbacks, new XunitTraceListener(outputHelper));

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(3));
        var cancellationToken = cancellationTokenSource.Token;

        _ = await client.BuildInitializeAsync(TestProjectPath.AspnetWithoutErrors, cancellationToken);
        await client.BuildInitializedAsync();

        // Act
        await client.ExitAsync();
        await buildServer.WaitForExitAsync(cancellationToken);

        // Assert
        Assert.Equal(1, buildServer.ExitCode);
    }

}