namespace dotnet_bsp;

public static class ProjectFinder
{
    public static string FindProjectForTarget(string targetPath)
    {
        var dir = Path.GetDirectoryName(targetPath.Replace("file://", ""));
        return FindInCurrentAndUpperDir(dir!);
    }

    private static string FindInCurrentAndUpperDir(string dir)
    {
        var directory = new DirectoryInfo(dir);
        foreach(var file in directory.GetFiles())
        {
            if (file.Extension == ".csproj")
            {
                return file.FullName;
            }
        }

        if (directory.Parent != null)
        {
            return FindInCurrentAndUpperDir(directory.Parent.FullName);
        }

        throw new FileNotFoundException($"csproj file not found");
    }
}