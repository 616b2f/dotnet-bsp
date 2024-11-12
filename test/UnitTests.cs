using bsp4csharp.Protocol;
using dotnet_bsp;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace test;

public partial class UnitTests
{
    private readonly ITestOutputHelper outputHelper;

    public UnitTests(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
    }

    [Fact]
    public async Task RequestInitializeBuild_Success()
    {
        // Arrange
        var testlogger = new UnitTestLogger(outputHelper);
        var buildServer = BuildServerFactory.CreateServer(testlogger);
        var client = buildServer.CreateClient();

        var cancelationTokenSource = new CancellationTokenSource();
        var initParams = new InitializeBuildParams
        {
            DisplayName = "TestClient",
            Version = "1.0.0",
            BspVersion = "2.1.1",
            RootUri = UriFixer.WithFileSchema(TestProjectPath.AspnetExample),
            Capabilities = new BuildClientCapabilities()
        };
        initParams.Capabilities.LanguageIds.Add("csharp");

        // Act
        var initResult = await client.SendRequestAsync<InitializeBuildParams, InitializeBuildResult>(Methods.BuildInitialize, initParams, cancelationTokenSource.Token);

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
            RootUri = UriFixer.WithFileSchema(TestProjectPath.AspnetExample),
            Capabilities = new BuildClientCapabilities()
        };
        initParams.Capabilities.LanguageIds.Add("csharp");

        var initResult = await client.SendRequestAsync<InitializeBuildParams, InitializeBuildResult>(Methods.BuildInitialize, initParams, cancelationTokenSource.Token);

        Assert.NotNull(initResult);

        var initializedParams = new InitializedBuildParams();
        await client.SendNotificationAsync<InitializedBuildParams>(Methods.BuildInitialized, initializedParams);

        Thread.Sleep(TimeSpan.FromSeconds(3));

        // Act
        var result = await client.SendRequestAsync<WorkspaceBuildTargetsResult>(Methods.WorkspaceBuildTargets, cancelationTokenSource.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.Targets.Count);
    }

    [Fact]
    public async Task Requests_BeforeInitializeBuildRequest_ShouldFail()
    {
        // // Arrange
        var testlogger = new UnitTestLogger(outputHelper);
        var buildServer = BuildServerFactory.CreateServer(testlogger);
        var client = buildServer.CreateClient();

        var cancelationTokenSource = new CancellationTokenSource();

        // Act
        var act = () => client.SendRequestAsync<WorkspaceBuildTargetsResult>(Methods.WorkspaceBuildTargets, cancelationTokenSource.Token);

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
            RootUri = UriFixer.WithFileSchema(TestProjectPath.AspnetExample),
            Capabilities = new BuildClientCapabilities()
        };
        initParams.Capabilities.LanguageIds.Add("csharp");

        var initResult = await client.SendRequestAsync<InitializeBuildParams, InitializeBuildResult>(Methods.BuildInitialize, initParams, cancelationTokenSource.Token);

        // Act
        var act = () => client.SendRequestAsync<WorkspaceBuildTargetsResult>(Methods.WorkspaceBuildTargets, cancelationTokenSource.Token);

        // Assert
        var exception = await Assert.ThrowsAsync<RemoteInvocationException>(act);

        Assert.Multiple(() =>
        {
            Assert.Equal(-32002, exception.ErrorCode); // ServerNotInitialized
            Assert.Equal("Client did not send Initialized notification", exception.Message);
        });
    }
}