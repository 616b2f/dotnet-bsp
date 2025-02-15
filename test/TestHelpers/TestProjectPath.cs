namespace test;


public static class TestProject
{
    public const string AspnetWithoutErrors = "aspnet-without-errors";
    public const string AspnetWithBuildErrors = "aspnet-with-build-errors";
    public const string AspnetWithRestoreErrors = "aspnet-with-restore-errors";
    public const string XunitTests = "xunit-tests";
    public const string NunitTests = "nunit-tests";
    public const string MsTestTests = "mstest-tests";
}

internal static class TestProjectPath
{
    private static readonly string TestProjectDir = Path.GetFullPath(AppContext.BaseDirectory + "../../../../test-projects");

    public static string AspnetWithoutErrors = GetFullPathFor(TestProject.AspnetWithoutErrors);
    public static string AspnetWithBuildErrors = GetFullPathFor(TestProject.AspnetWithBuildErrors);
    public static string XunitTests = GetFullPathFor(TestProject.XunitTests);
    public static string NunitTests = GetFullPathFor(TestProject.NunitTests);
    public static string MsTestTests = GetFullPathFor(TestProject.MsTestTests);

    public static string GetFullPathFor(string testProjectDir, string projectName)
    {
        return Path.Combine(testProjectDir, projectName);
    }

    public static string GetFullPathFor(string projectName)
    {
        return Path.Combine(TestProjectDir, projectName);
    }
}