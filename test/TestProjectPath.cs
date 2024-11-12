namespace test;

public partial class UnitTests
{
    internal static class TestProjectPath
    {
        private static string TestProjectDir = Path.GetFullPath(AppContext.BaseDirectory + "../../../../test-projects");

        public static string AspnetExample = TestProjectDir + "/aspnet-example";
    }
}