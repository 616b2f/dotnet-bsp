using bsp4csharp.Protocol;
using dotnet_bsp;

namespace test;

public static class BuildServerClientExtensions
{
    public static Task<InitializeBuildResult> BuildInitializeAsync(this BuildServerClient client, string workspaceRootPath, CancellationToken cancellationToken)
    {
        var initParams = new InitializeBuildParams
        {
            DisplayName = "TestClient",
            Version = "1.0.0",
            BspVersion = "2.1.1",
            RootUri = UriFixer.WithFileSchema(workspaceRootPath),
            Capabilities = new BuildClientCapabilities()
        };
        initParams.Capabilities.LanguageIds.Add("csharp");

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

    public static Task ShutdownAsync(this BuildServerClient client)
    {
        return client.SendRequestAsync(Methods.BuildShutdown);
    }

    public static Task ExitAsync(this BuildServerClient client)
    {
        return client.SendNotificationAsync(Methods.BuildExit);
    }
}