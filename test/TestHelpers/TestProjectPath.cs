namespace test;

internal static class TestProjectPath
{
    private static readonly string TestProjectDir = Path.GetFullPath(AppContext.BaseDirectory + "../../../../test-projects");

    public static string AspnetExample = TestProjectDir + "/aspnet-example";
    public static string AspnetWithErrors = TestProjectDir + "/aspnet-with-errors";
}