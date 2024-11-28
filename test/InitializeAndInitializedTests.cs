using StreamJsonRpc;
using Xunit.Abstractions;

namespace test;

public partial class InitializeAndInitializedTests : IAsyncLifetime
{
    private readonly CancellationToken _cancellationToken;
    private readonly BuildServerClient _client;
    private readonly TestBuildServer _buildServer;

    public InitializeAndInitializedTests(ITestOutputHelper outputHelper)
    {
        var testlogger = new UnitTestLogger(outputHelper);
        _buildServer = BuildServerFactory.CreateServer(testlogger);
        var serverCallbacks = new ServerCallbacks(outputHelper);
        _client = _buildServer.CreateClient(serverCallbacks);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(3));
        _cancellationToken = cancellationTokenSource.Token;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RequestInitializeBuild_Success()
    {
        // Arrange

        // Act
        var initResult = await _client.BuildInitializeAsync(TestProjectPath.AspnetWithoutErrors, _cancellationToken);

        // Assert
        Assert.Equal("dotnet-bsp", initResult.DisplayName);
        Assert.Equal("2.1.1", initResult.BspVersion);
        Assert.Equal("0.0.1", initResult.Version);
        Assert.NotNull(initResult.Capabilities.CompileProvider);
        Assert.Contains("csharp", initResult.Capabilities.CompileProvider.LanguageIds);
        Assert.NotNull(initResult.Capabilities.RunProvider);
        Assert.Contains("csharp", initResult.Capabilities.RunProvider.LanguageIds);
        Assert.NotNull(initResult.Capabilities.TestProvider);
        Assert.Contains("csharp", initResult.Capabilities.TestProvider.LanguageIds);
        Assert.NotNull(initResult.Capabilities.TestCaseDiscoveryProvider);
        Assert.Contains("csharp", initResult.Capabilities.TestCaseDiscoveryProvider.LanguageIds);
    }

    [Fact]
    public async Task Requests_BeforeInitializeBuildRequest_ShouldFail()
    {
        // Arrange
        // Act
        var act = () => _client.WorkspaceBuildTargetsAsync(_cancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<RemoteInvocationException>(act);

        Assert.Multiple(() =>
        {
            Assert.Equal("Server not Initialized", exception.Message);
            Assert.Equal(-32002, exception.ErrorCode); // ServerNotInitialized
        });
    }

    [Fact]
    public async Task Requests_BeforeInitializedBuildNotification_ShouldFail()
    {
        // Arrange
        _ = await _client.BuildInitializeAsync(TestProjectPath.AspnetWithoutErrors, _cancellationToken);

        // Act
        var act = () => _client.WorkspaceBuildTargetsAsync(_cancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<RemoteInvocationException>(act);

        Assert.Multiple(() =>
        {
            Assert.Equal(-32002, exception.ErrorCode); // ServerNotInitialized
            Assert.Equal("Client did not send Initialized notification", exception.Message);
        });
    }

    public async Task DisposeAsync()
    {
        await _client.ShutdownAsync();
        await _client.ExitAsync();
        _client.Dispose();
        await _buildServer.WaitForExitAsync(_cancellationToken);
    }
}