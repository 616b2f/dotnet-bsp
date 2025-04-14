using BaseProtocol;
using bsp4csharp.Protocol;
using dotnet_bsp.Logging;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Graph;

namespace dotnet_bsp;

internal static class BuildHelper
{
    internal static IEnumerable<BuildTargetIdentifier> FilterProjectsOutIfPartOfAnSolutionTarget(BuildTargetIdentifier[] targets)
    {
        var filteredTargets = targets.ToList();
        var slnList = targets
            .Where(x => Path.GetExtension(x.ToString()) == ".sln");
        var projList = targets
            .Where(x => Path.GetExtension(x.ToString()) == ".csproj")
            .Select(x => x.Uri.AbsolutePath)
            .ToList();
        foreach (var target in slnList)
        {
            var slnFile = SolutionFile.Parse(target.ToString());
            if (slnFile is not null)
            {
                var projectFilesInSln = slnFile.ProjectsInOrder
                    .Select(x => x.AbsolutePath);

                var includedProj = projectFilesInSln
                    .Intersect(projList);

                filteredTargets
                    .RemoveAll(x => includedProj.Contains(x.Uri.AbsolutePath));
            }
        }

        return filteredTargets;
    }

    internal static bool RestoreTestTargets(
        IEnumerable<string> targetFiles,
        ProjectCollection projects,
        IBpLogger logger,
        MSBuildLogger msBuildLogger)
    {
        bool restoreResult = false;
        var graph = new ProjectGraph(targetFiles, projects);
        var testProjects = graph.ProjectNodesTopologicallySorted
            .Where(x => x.ProjectInstance.IsTestProject());
        foreach (var proj in testProjects)
        {
            var globalProps = proj.ProjectInstance.GlobalProperties
                .Select(x => string.Format("{0}={1}", x.Key, x.Value))
                .ToArray();
            logger.LogInformation("Global Properties: {}", string.Join("\n", globalProps));
            logger.LogInformation("Start restore target: {}", proj.ProjectInstance.FullPath);
            var result = proj.ProjectInstance.Build(["Restore"], [msBuildLogger]);
            logger.LogInformation($"{proj.ProjectInstance.FullPath} restore result: {result}");
            restoreResult &= result;
        }

        return restoreResult;
    }

    internal static bool BuildTestTargets(
        IEnumerable<string> targetFiles,
        ProjectCollection projects,
        IBpLogger logger,
        MSBuildLogger msBuildLogger)
    {
        bool buildResult = false;
        var graph = new ProjectGraph(targetFiles, projects);
        var testProjects = graph.ProjectNodesTopologicallySorted
            .Where(x => x.ProjectInstance.IsTestProject());
        foreach (var projNode in testProjects)
        {
            logger.LogInformation("Start building target: {}", projNode.ProjectInstance.FullPath);
            var result = projNode.ProjectInstance.Build(["Build"], [msBuildLogger]);
            logger.LogInformation($"{projNode.ProjectInstance.FullPath} build result: {result}");
            buildResult &= result;
        }

        return buildResult;
    }
}