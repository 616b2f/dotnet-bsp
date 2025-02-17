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
                        Id = "7cb68ff645bdbfb79d50da3baa24cc52",
                        FullyQualifiedName = "mstest_tests.Test1.Test1_Success",
                        DisplayName = "Test1_Success (\"a\")",
                        FilePath = Path.Combine(TestProjectPath.MsTestTests, "Test1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin", "Debug", "net8.0", "mstest-tests.dll"),
                        Line = 10,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.MsTestTests, "mstest-tests.csproj")) }
                    },
                    new() {
                        Id = "7bf6fe0f0a7eadd3853ab80bcc0e08c8",
                        FullyQualifiedName = "mstest_tests.Test1.Test1_Success",
                        DisplayName = "Test1_Success (\"b\")",
                        FilePath = Path.Combine(TestProjectPath.MsTestTests, "Test1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin", "Debug", "net8.0", "mstest-tests.dll"),
                        Line = 10,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.MsTestTests, "mstest-tests.csproj")) }
                    },
                    new() {
                        Id = "18b4d237df0079641bff90d85d47fc44",
                        FullyQualifiedName = "mstest_tests.Test1.TestMethod2",
                        DisplayName = "TestMethod2",
                        FilePath = Path.Combine(TestProjectPath.MsTestTests, "Test1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin", "Debug", "net8.0", "mstest-tests.dll"),
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
                        Id = "fed573ba74557a2b4bac775a81364a55",
                        FullyQualifiedName = "xunit_tests.UnitTest1.Test1_Success",
                        DisplayName = "xunit_tests.UnitTest1.Test1_Success(expectedValue: \"a\")",
                        FilePath = Path.Combine(TestProjectPath.XunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin", "Debug", "net8.0", "xunit-tests.dll"),
                        Line = 9,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.XunitTests, "xunit-tests.csproj")) }
                    },
                    new() {
                        Id = "ad63df2419d6468c651b48434dca4f7f",
                        FullyQualifiedName = "xunit_tests.UnitTest1.Test1_Success",
                        DisplayName = "xunit_tests.UnitTest1.Test1_Success(expectedValue: \"b\")",
                        FilePath = Path.Combine(TestProjectPath.XunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin", "Debug", "net8.0", "xunit-tests.dll"),
                        Line = 9,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.XunitTests, "xunit-tests.csproj")) }
                    },
                    new() {
                        Id = "97e017e6d23eeaea15aec7c9f7a6f7e1",
                        FullyQualifiedName = "xunit_tests.UnitTest1.Test2",
                        DisplayName = "xunit_tests.UnitTest1.Test2",
                        FilePath = Path.Combine(TestProjectPath.XunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin", "Debug", "net8.0", "xunit-tests.dll"),
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
                        Id = "0adf3ecf65aa39bf8c2cef8e32d00c4d",
                        FullyQualifiedName = "nunit_tests.Tests.Test1",
                        DisplayName = "Test1",
                        FilePath = Path.Combine(TestProjectPath.NunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin", "Debug", "net8.0", "nunit-tests.dll"),
                        Line = 19,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.NunitTests, "nunit-tests.csproj")) }
                    },
                    new() {
                        Id = "184508b7d754ea96bb09cfe0c139881d",
                        FullyQualifiedName = "nunit_tests.Tests.Test1_Success(\"a\")",
                        DisplayName = "Test1_Success(\"a\")",
                        FilePath = Path.Combine(TestProjectPath.NunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin", "Debug", "net8.0", "nunit-tests.dll"),
                        Line = 13,
                        BuildTarget = new BuildTargetIdentifier{ Uri = UriFixer.WithFileSchema(Path.Combine(TestProjectPath.NunitTests, "nunit-tests.csproj")) }
                    },
                    new() {
                        Id = "4afdceb43dc9b939b2538d3b98bbeb94",
                        FullyQualifiedName = "nunit_tests.Tests.Test1_Success(\"b\")",
                        DisplayName = "Test1_Success(\"b\")",
                        FilePath = Path.Combine(TestProjectPath.NunitTests, "UnitTest1.cs"),
                        Source = Path.Combine(TestProjectPath.MsTestTests, "bin", "Debug", "net8.0", "nunit-tests.dll"),
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

        CleanupOutputDirectories(testProjectPath);

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

            Assert.True(expected.Id == actual!.Id, $"{testProjectName}:{expected.DisplayName}: expected: {expected.Id} actual: {actual.Id}");
            Assert.Equal(expected.Source, actual!.Source);
            Assert.Equal(expected.FilePath, actual.FilePath);
            Assert.Equal(expected.BuildTarget.Uri, actual.BuildTarget.Uri);
            Assert.True(expected.Line == actual!.Line, $"{testProjectName}:{expected.DisplayName}: expected: {expected.Line} actual: {actual.Line}");
            Assert.True(expected.FullyQualifiedName == actual!.FullyQualifiedName, $"{testProjectName}:{expected.DisplayName}: expected: {expected.FullyQualifiedName} actual: {actual.FullyQualifiedName}");
            Assert.True(expected.DisplayName == actual!.DisplayName, $"{testProjectName}:{expected.DisplayName}: expected: {expected.DisplayName} actual: {actual.DisplayName}");
        }

        // Assert.Equivalent(expectedTestCaseDiscoveredData, discoveredTestCases);
    }

    private void CleanupOutputDirectories(string testProjectPath)
    {
        string[] dirs = ["bin", "obj"];
        foreach (var dir in dirs)
        {
            var path = Path.Combine(testProjectPath, dir);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

    }

    public async Task DisposeAsync()
    {
        await _client.ShutdownAsync();
        await _client.ExitAsync();
        _client.Dispose();
        await _buildServer.WaitForExitAsync(_cancellationToken);
    }
}