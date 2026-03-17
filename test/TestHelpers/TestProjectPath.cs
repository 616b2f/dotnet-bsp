using bsp4csharp.Protocol;
using dotnet_bsp;

namespace test;

public static class TestProject
{
    public const string AspnetWithoutErrors = "aspnet-without-errors";
    public const string AspnetWithBuildErrors = "aspnet-with-build-errors";
    public const string AspnetWithRestoreErrors = "aspnet-with-restore-errors";
    public const string XunitTests = "xunit-tests";
    public const string NunitTests = "nunit-tests";
    public const string MsTestTests = "mstest-tests";
    public const string MsTestSlnTests = "mstest-sln-tests";
}

internal static class TestData
{
    internal static InitializeBuildParams GetInitParams(string workspaceRootPath)
    {
        return new InitializeBuildParams
        {
            DisplayName = "TestClient",
            Version = "1.0.0",
            BspVersion = "2.1.1",
            RootUri = UriFixer.WithFileSchema(workspaceRootPath),
            Capabilities = new BuildClientCapabilities
            {
                LanguageIds = ["csharp"]
            }
        };
    }
}

internal static class TestProjectPath
{
    private static readonly string TestProjectDir = Path.GetFullPath(AppContext.BaseDirectory + "../../../../test-projects");

    public static string AspnetWithoutErrors = GetFullPathFor(TestProject.AspnetWithoutErrors);
    public static string AspnetWithBuildErrors = GetFullPathFor(TestProject.AspnetWithBuildErrors);
    public static string XunitTests = GetFullPathFor(TestProject.XunitTests);
    public static string NunitTests = GetFullPathFor(TestProject.NunitTests);
    public static string MsTestTests = GetFullPathFor(TestProject.MsTestTests);
    public static string MsTestSlnTests = GetFullPathFor(TestProject.MsTestSlnTests);

    public static string GetFullPathFor(string testProjectDir, string projectName)
    {
        return Path.Combine(testProjectDir, projectName);
    }

    public static string GetFullPathFor(string projectName)
    {
        return Path.Combine(TestProjectDir, projectName);
    }
}