using bsp4csharp.Protocol;
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

        var firstTarget = result.Targets.First();
        Assert.Multiple(() =>
        {
            Assert.Equal("AspNet.Example.sln", firstTarget.DisplayName);
            Assert.Equal($"file://{TestProjectPath.AspnetExample}/AspNet.Example.sln", firstTarget.Id.Uri.ToString());
            Assert.NotNull(firstTarget.BaseDirectory);
            Assert.Equal($"file://{TestProjectPath.AspnetExample}", firstTarget.BaseDirectory.ToString());
            Assert.True(firstTarget.Capabilities.CanCompile, "CanCompile is false");
            Assert.True(firstTarget.Capabilities.CanTest, "CanTest is false");
            Assert.False(firstTarget.Capabilities.CanRun, "CanRun is true");
            Assert.False(firstTarget.Capabilities.CanDebug, "CanDebug is true");
            Assert.Equal(["csharp"], firstTarget.LanguageIds);
            Assert.Equal([], firstTarget.Tags);
            Assert.Null(firstTarget.Data);
            Assert.Null(firstTarget.DataKind);
            // Assert.Equal([], firstTarget.Dependencies);
        });
    }

    [Fact]
    public async Task RequestBuildTargetCompile_ForProjectWithErrors_Success()
    {
        // Arrange
        _ = await _client.BuildInitializeAsync(TestProjectPath.AspnetWithErrors, _cancellationToken);
        await _client.BuildInitializedAsync();

        // Act
        var buildTargets = await _client.WorkspaceBuildTargetsAsync(_cancellationToken);

        // Assert
        Assert.NotNull(buildTargets);
        var slnTarget = buildTargets.Targets.First(x => x.DisplayName == "AspNet.Example.sln");

        var expectedOriginId = Guid.NewGuid().ToString();

        var compileParams = new CompileParams
        {
            Targets = [slnTarget.Id],
            OriginId = expectedOriginId
        };
        var result = await _client.CompileAsync(compileParams, _cancellationToken);

        Assert.Equal(expectedOriginId, result.OriginId);
        Assert.Equal(StatusCode.Error, result.StatusCode);
    }


    public async Task DisposeAsync()
    {
        await _client.ShutdownAsync();
        await _client.ExitAsync();
        _client.Dispose();
        await _buildServer.WaitForExitAsync(_cancellationToken);
    }
}