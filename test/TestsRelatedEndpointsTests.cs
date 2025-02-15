using bsp4csharp.Protocol;
using dotnet_bsp;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace test;

public partial class TestsRelatedEndpointsTests : IAsyncLifetime
{
    private readonly CancellationToken _cancellationToken;
    private readonly BuildServerClient _client;
    private readonly TestBuildServer _buildServer;
    private readonly ServerCallbacks _serverCallbacks;
    private ITestOutputHelper _outputHelper;

    public TestsRelatedEndpointsTests(ITestOutputHelper outputHelper)
    {
        // System.Environment.CurrentDirectory
        _outputHelper = outputHelper;
        var testlogger = new UnitTestLogger(outputHelper);
        _buildServer = BuildServerFactory.CreateServer(testlogger);
        _serverCallbacks = new ServerCallbacks(outputHelper);
        _client = _buildServer.CreateClient(_serverCallbacks);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(20040));
        _cancellationToken = cancellationTokenSource.Token;
    }

    public Task InitializeAsync()
    {
        // TODO: cleanup all bin/ and obj/ folders before each run
        return Task.CompletedTask;
    }

    public static IEnumerable<object[]> TestDataTestCaseDiscovery()
    {
        return new List<object[]>
        {
            new object[]
            {
                TestProject.MsTestTests,
                new List<TestCaseDiscoveredData>
                {
                    new() {
                        FullyQualifiedName = "mstest_tests.Test1.Test1_Success",
                        DisplayName = "Test1_Success (\"a\")",
                        FilePath = Path.Combine(TestProjectPath.MsTestTests, "Test1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin/Debug/net8.0/mstest-tests.dll"),
                        Line = 10,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.MsTestTests, "mstest-tests.csproj")) }
                    },
                    new() {
                        FullyQualifiedName = "mstest_tests.Test1.Test1_Success",
                        DisplayName = "Test1_Success (\"b\")",
                        FilePath = Path.Combine(TestProjectPath.MsTestTests, "Test1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin/Debug/net8.0/mstest-tests.dll"),
                        Line = 10,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.MsTestTests, "mstest-tests.csproj")) }
                    },
                    new() {
                        FullyQualifiedName = "mstest_tests.Test1.TestMethod2",
                        DisplayName = "TestMethod2",
                        FilePath = Path.Combine(TestProjectPath.MsTestTests, "Test1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin/Debug/net8.0/mstest-tests.dll"),
                        Line = 16,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.MsTestTests, "mstest-tests.csproj")) }
                    }
                }
            },
            new object[]
            {
                TestProject.XunitTests,
                new List<TestCaseDiscoveredData>
                {
                    new() {
                        FullyQualifiedName = "xunit_tests.UnitTest1.Test1_Success",
                        DisplayName = "xunit_tests.UnitTest1.Test1_Success(expectedValue: \"a\")",
                        FilePath = Path.Combine(TestProjectPath.XunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.XunitTests, "bin/Debug/net8.0/xunit-tests.dll"),
                        Line = 9,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.XunitTests, "xunit-tests.csproj")) }
                    },
                    new() {
                        FullyQualifiedName = "xunit_tests.UnitTest1.Test1_Success",
                        DisplayName = "xunit_tests.UnitTest1.Test1_Success(expectedValue: \"b\")",
                        FilePath = Path.Combine(TestProjectPath.XunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.XunitTests, "bin/Debug/net8.0/xunit-tests.dll"),
                        Line = 9,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.XunitTests, "xunit-tests.csproj")) }
                    },
                    new() {
                        FullyQualifiedName = "xunit_tests.UnitTest1.Test2",
                        DisplayName = "xunit_tests.UnitTest1.Test2",
                        FilePath = Path.Combine(TestProjectPath.XunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.XunitTests, "bin/Debug/net8.0/xunit-tests.dll"),
                        Line = 15,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.XunitTests, "xunit-tests.csproj")) }
                    }
                }
            },
            new object[]
            {
                TestProject.NunitTests,
                new List<TestCaseDiscoveredData>
                {
                    new() {
                        FullyQualifiedName = "nunit_tests.Tests.Test1",
                        DisplayName = "Test1",
                        FilePath = Path.Combine(TestProjectPath.NunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.NunitTests, "bin/Debug/net8.0/nunit-tests.dll"),
                        Line = 19,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.NunitTests, "nunit-tests.csproj")) }
                    },
                    new() {
                        FullyQualifiedName = "nunit_tests.Tests.Test1_Success(\"a\")",
                        DisplayName = "Test1_Success(\"a\")",
                        FilePath = Path.Combine(TestProjectPath.NunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.NunitTests, "bin/Debug/net8.0/nunit-tests.dll"),
                        Line = 13,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.NunitTests, "nunit-tests.csproj")) }
                    },
                    new() {
                        FullyQualifiedName = "nunit_tests.Tests.Test1_Success(\"b\")",
                        DisplayName = "Test1_Success(\"b\")",
                        FilePath = Path.Combine(TestProjectPath.NunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.NunitTests, "bin/Debug/net8.0/nunit-tests.dll"),
                        Line = 13,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.NunitTests, "nunit-tests.csproj")) }
                    },
                }
            },
        };
    }

    [Theory]
    [MemberData(nameof(TestDataTestCaseDiscovery))]
    public async Task RequestBuildTargetTestCaseDiscovery_ForFramework_Success(string testProjectName, IList<TestCaseDiscoveredData> expectedTestCaseDiscoveredData)
    {
        var testProjectPath = TestProjectPath.GetFullPathFor(testProjectName);
        _ = await _client.BuildInitializeAsync(testProjectPath, _cancellationToken);
        await _client.BuildInitializedAsync();

        var buildTargets = await _client.WorkspaceBuildTargetsAsync(_cancellationToken);
        Assert.NotNull(buildTargets);

        var testTarget = buildTargets.Targets.Single(x => x.Capabilities.CanTest == true);
        Assert.NotNull(testTarget);

        var expectedOriginId = Guid.NewGuid().ToString();

        var testCaseDiscoveryParams = new TestCaseDiscoveryParams
        {
            Targets = [testTarget.Id],
            OriginId = expectedOriginId
        };

        // Act
        var result = await _client.BuildTargetTestCaseDiscoveryAsync(testCaseDiscoveryParams, _cancellationToken);

        // Assert
        Assert.Equal(expectedOriginId, result.OriginId);
        Assert.Equal(StatusCode.Ok, result.StatusCode);
        var taskStart = _serverCallbacks.TaskNotifications
            .OfType<TaskStartParams>()
            .SingleOrDefault(x => x.DataKind == TaskStartDataKind.TestCaseDiscoveryTask);
        Assert.NotNull(taskStart);
        var taskFinish = _serverCallbacks.TaskNotifications
            .OfType<TaskFinishParams>()
            .SingleOrDefault(x => x.DataKind == TaskFinishDataKind.TestCaseDiscoveryFinish);
        Assert.NotNull(taskFinish);
        var tasksProcessing = _serverCallbacks.TaskNotifications
            .OfType<TaskProgressParams>()
            .Where(x => x.DataKind == TaskProgressDataKind.TestCaseDiscovered);
        Assert.Equal(expectedTestCaseDiscoveredData.Count(), tasksProcessing.Count());

        Assert.All(tasksProcessing, x => Assert.IsType<JObject>(x.Data));

        var discoveredTestCases = tasksProcessing
            .Select(x => x.Data)
            .OfType<JObject>()
            .Select(x => x.ToObject<TestCaseDiscoveredData>())
            .ToList();

        // Arrange
        for (var i = 0; i < expectedTestCaseDiscoveredData.Count; i++)
        {
            var expected = expectedTestCaseDiscoveredData[i];
            var actual = discoveredTestCases[i];

            Assert.Equal(expected.Source, actual!.Source);
            Assert.Equal(expected.FilePath, actual.FilePath);
            Assert.Equal(expected.BuildTarget.Uri, actual.BuildTarget.Uri);
            Assert.True(expected.Line == actual!.Line, $"{testProjectName}:{expected.DisplayName}: expected: {expected.Line} actual: {actual.Line}");
            Assert.True(expected.FullyQualifiedName == actual!.FullyQualifiedName, $"{testProjectName}:{expected.DisplayName}: expected: {expected.FullyQualifiedName} actual: {actual.FullyQualifiedName}");
            Assert.True(expected.DisplayName == actual!.DisplayName, $"{testProjectName}:{expected.DisplayName}: expected: {expected.DisplayName} actual: {actual.DisplayName}");
        }

        // Assert.Equivalent(expectedTestCaseDiscoveredData, discoveredTestCases);
    }

    public async Task DisposeAsync()
    {
        await _client.ShutdownAsync();
        await _client.ExitAsync();
        _client.Dispose();
        await _buildServer.WaitForExitAsync(_cancellationToken);
    }
}