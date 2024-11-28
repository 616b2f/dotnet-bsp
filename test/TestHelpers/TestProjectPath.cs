namespace test;


public static class TestProject
{
    public const string AspnetWithoutErrors = "aspnet-without-errors";
    public const string AspnetWithBuildErrors = "aspnet-with-build-errors";
    public const string AspnetWithRestoreErrors = "aspnet-with-restore-errors";
}

internal static class TestProjectPath
{
    private static readonly string TestProjectDir = Path.GetFullPath(AppContext.BaseDirectory + "../../../../test-projects");

    public static string AspnetWithoutErrors = GetFullPathFor(TestProject.AspnetWithoutErrors);
    public static string AspnetWithBuildErrors = GetFullPathFor(TestProject.AspnetWithBuildErrors);

    public static string GetFullPathFor(string projectName)
    {
        return TestProjectDir + "/" + projectName;
    }
}