using StreamJsonRpc;
using Xunit.Abstractions;

namespace test;

public partial class BuildServerProtocolTests : IAsyncLifetime
{
    private readonly CancellationToken _cancellationToken;
    private readonly BuildServerClient _client;
    private readonly TestBuildServer _buildServer;

    public BuildServerProtocolTests(ITestOutputHelper outputHelper)
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
        var initResult = await _client.BuildInitializeAsync(TestProjectPath.AspnetExample, _cancellationToken);

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
    public async Task RequestWorkspaceBuildTargets_AfterInitialize_Success()
    {
        // Arrange
        _ = await _client.BuildInitializeAsync(TestProjectPath.AspnetExample, _cancellationToken);
        await _client.BuildInitializedAsync();

        // Act
        var result = await _client.WorkspaceBuildTargetsAsync(_cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.Targets.Count);
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
        _ = await _client.BuildInitializeAsync(TestProjectPath.AspnetExample, _cancellationToken);

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