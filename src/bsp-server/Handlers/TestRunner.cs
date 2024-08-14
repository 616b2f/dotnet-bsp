using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using BaseProtocol;

namespace dotnet_bsp.Handlers;

public static class TestRunner
{
    public record SdkVersion(int Major, int Minor, int Patch, string DirPath);

    public static string? FindVsTestConsole()
    {
        var userDir = Environment.ExpandEnvironmentVariables("%HOME%/.dotnet");
        string[] dirs = [
            userDir,
            "/usr/lib/dotnet/sdk",
            "/usr/lib64/dotnet/sdk",
            "/usr/share/dotnet/sdk"
        ];

        var versions = new List<SdkVersion>();
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var sdkdir in Directory.GetDirectories(dir))
            {
                var rex = new Regex(@"(\d*)\.(\d*)\.(\d*)");
                var match = rex.Match(sdkdir);
                if (match.Success)
                {
                    versions.Add(
                        new SdkVersion(
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value),
                            int.Parse(match.Groups[3].Value),
                            sdkdir
                        )
                    );
                }
            }
        }

        var highestVersion = versions
            .OrderByDescending(x => x.Major)
            .ThenByDescending(x => x.Minor)
            .ThenByDescending(x => x.Patch)
            .FirstOrDefault();

        if (highestVersion is not null)
        {
            return Path.Combine(highestVersion.DirPath, "vstest.console.dll");
        }

        return null;
    }

    public static string? FindTestAdapter(Project proj, RequestContext context)
    {
        var targetPath = proj.Properties.First(x => x.Name == "TargetPath").EvaluatedValue;
        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (targetDirectory is null)
        {
            context.Logger.LogError("can't get root directory of target: {}", targetPath);
            return null;
        }

        var testAdapterPath = Directory.GetFiles(targetDirectory)
            .FirstOrDefault(x => x.EndsWith(".testadapter.dll", StringComparison.InvariantCultureIgnoreCase));

        if (testAdapterPath is null)
        {
            context.Logger.LogError("Can't find testadapter in path: {}", targetDirectory);
            return null;
        }

        context.Logger.LogInformation("test adapter found: {}", testAdapterPath);
        return testAdapterPath;
    }
}

