using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace dotnet_bsp;

public static class SolutionExtensions
{
    public static ICollection<Project> GetProjects(this SolutionFile solutionFilePath)
    {
        var configurationName = solutionFilePath.GetDefaultConfigurationName();
        var platformName = solutionFilePath.GetDefaultPlatformName();
        var projects = new ProjectCollection();
        if (string.Equals(platformName, "Any CPU", StringComparison.InvariantCultureIgnoreCase))
        {
            platformName = "AnyCpu";
        }

        var projectFilesInSln = solutionFilePath.ProjectsInOrder
            .Where(x => 
                (x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat ||
                x.ProjectType == SolutionProjectType.WebProject) &&
                // and only projects that has the build flag enabled for the provided configuration
                x.ProjectConfigurations.Values.Any(v =>
                    v.ConfigurationName.Equals(configurationName, StringComparison.InvariantCultureIgnoreCase) &&
                    v.PlatformName.Equals(platformName, StringComparison.InvariantCultureIgnoreCase) &&
                    v.IncludeInBuild)
                )
            .Select(x => x.AbsolutePath);

        foreach (var projectFile in projectFilesInSln)
        {
            projects.LoadProject(projectFile);
        }

        return projects.LoadedProjects;
    }
}