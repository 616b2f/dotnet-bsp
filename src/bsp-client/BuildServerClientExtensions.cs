using System.Threading;
using System.Threading.Tasks;
using bsp4csharp.Protocol;

namespace bsp_client;

public static class BuildServerClientExtensions
{
    public static Task<InitializeBuildResult> BuildInitializeAsync(this BuildServerClient client, InitializeBuildParams initParams, CancellationToken cancellationToken)
    {
        return client.SendRequestAsync<InitializeBuildParams, InitializeBuildResult>(Methods.BuildInitialize, initParams, cancellationToken);
    }

    public static Task BuildInitializedAsync(this BuildServerClient client)
    {
        var initializedParams = new InitializedBuildParams();
        return client.SendNotificationAsync(Methods.BuildInitialized, initializedParams);
    }

    public static Task<WorkspaceBuildTargetsResult> WorkspaceBuildTargetsAsync(this BuildServerClient client, CancellationToken cancellationToken)
    {
        return client.SendRequestAsync<WorkspaceBuildTargetsResult>(Methods.WorkspaceBuildTargets, cancellationToken);
    }

    public static Task<CompileResult> CompileAsync(this BuildServerClient client, CompileParams compileParams, CancellationToken cancellationToken)
    {
        return client.SendRequestAsync<CompileParams, CompileResult>(Methods.BuildTargetCompile, compileParams, cancellationToken);
    }

    public static Task<TestCaseDiscoveryResult> BuildTargetTestCaseDiscoveryAsync(this BuildServerClient client, TestCaseDiscoveryParams testCaseDiscoveryParams, CancellationToken cancellationToken)
    {
        return client.SendRequestAsync<TestCaseDiscoveryParams, TestCaseDiscoveryResult>(Methods.BuildTargetTestCaseDiscovery, testCaseDiscoveryParams, cancellationToken);
    }

    public static Task<TestResult> BuildTargetTestAsync(this BuildServerClient client, TestParams testParams, CancellationToken cancellationToken)
    {
        return client.SendRequestAsync<TestParams, TestResult>(Methods.BuildTargetTest, testParams, cancellationToken);
    }

    public static Task ShutdownAsync(this BuildServerClient client)
    {
        return client.SendRequestAsync(Methods.BuildShutdown);
    }

    public static Task ExitAsync(this BuildServerClient client)
    {
        return client.SendNotificationAsync(Methods.BuildExit);
    }
}