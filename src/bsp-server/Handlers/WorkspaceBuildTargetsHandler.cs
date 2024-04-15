using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.WorkspaceBuildTargets)]
internal class WorkspaceBuildTargetsHandler
    : IRequestHandler<WorkspaceBuildTargetsResult, RequestContext>
{
    private readonly IInitializeManager<InitializeBuildParams, InitializeBuildResult> _capabilitiesManager;
    private readonly string PACKAGE_REFERENCE_TAG = "PackageReference";

    public WorkspaceBuildTargetsHandler(IInitializeManager<InitializeBuildParams, InitializeBuildResult> capabilitiesManager)
    {
        _capabilitiesManager = capabilitiesManager;
    }

    public bool MutatesSolutionState => false;

    public Task<WorkspaceBuildTargetsResult> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
    {
        var list = new List<BuildTarget>();
        var initParams = _capabilitiesManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var workspacePath = initParams.RootUri.AbsolutePath;
            var buildTargets = GetBuildTargetsInWorkspace(workspacePath, context.Logger);
            list.AddRange(buildTargets);
        }
        return Task.FromResult(new WorkspaceBuildTargetsResult
        {
            Targets = list
        });
    }

    private IReadOnlyList<BuildTarget> GetBuildTargetsInWorkspace(string workspacePath, IBpLogger logger)
    {
        var list = new List<BuildTarget>();
        logger.LogInformation("Search solutin files in: {}", workspacePath);
        //TODO: think about the implications to search for all sln files in workspace
        var slnFilePath = Directory.GetFiles(workspacePath, "*.sln").Take(1).SingleOrDefault();
        var projectFiles = new List<string>();
        if (!string.IsNullOrWhiteSpace(slnFilePath))
        {
            logger.LogInformation("Found solution file: {}", slnFilePath);
            var buildTarget = new BuildTarget
            {
                Id = new BuildTargetIdentifier
                {
                    Uri = new System.Uri(slnFilePath, UriKind.Relative)
                },
                DisplayName = Path.GetFileName(slnFilePath),
                Capabilities = new BuildTargetCapabilities { CanCompile = true }
            };
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(slnFilePath));
            if (baseDirectory != null)
            {
                buildTarget.BaseDirectory = new System.Uri(baseDirectory);
            }

            list.Add(buildTarget);

            var sln = SolutionFile.Parse(slnFilePath);
            var projectFilesInSln = sln.ProjectsInOrder
                .Where(x => 
                    x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat ||
                    x.ProjectType == SolutionProjectType.WebProject)
                .Select(x => x.AbsolutePath);
            projectFiles.AddRange(projectFilesInSln);
        }
        else
        {
            logger.LogInformation("Search for project files in: {}", workspacePath);
            projectFiles.AddRange(Directory.GetFiles(workspacePath, "*.csproj"));
        }

        logger.LogInformation("Found project files: {}",
            string.Join(Environment.NewLine, projectFiles));
        var projectCollection = new ProjectCollection();
        var projects = projectFiles
            .Select(x => Project.FromFile(x,
                new Microsoft.Build.Definition.ProjectOptions
                {
                    ProjectCollection = projectCollection,
                    ToolsVersion = projectCollection.DefaultToolsVersion,
                }))
            .ToArray();
        list.AddRange(ProjectsToBuildTargets(projects));

        return list;
    }

    private IEnumerable<BuildTarget> ProjectsToBuildTargets(IReadOnlyList<Project> projects)
    {
        var list = new List<BuildTarget>();
        foreach (var project in projects)
        {
            var outputType = project.GetProperty("OutputType");
            var canRun = outputType?.EvaluatedValue.Equals("Exe", StringComparison.OrdinalIgnoreCase) ?? true;
            var canTest = project.AllEvaluatedItems.Any(item => 
                item.ItemType.Equals(PACKAGE_REFERENCE_TAG, StringComparison.OrdinalIgnoreCase) &&
                item.EvaluatedInclude.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
            var tags = new List<BuildTargetTag>();
            //TODO: mark Libraries with BuildTargetTag.Library tag
            if (canRun)
            {
                if (canTest)
                {
                    tags.Add(BuildTargetTag.Test);
                } else {
                    tags.Add(BuildTargetTag.Application);
                }
            }

            var buildTarget = new BuildTarget
            {
                Id = new BuildTargetIdentifier
                {
                    Uri = new System.Uri(project.FullPath, UriKind.Absolute)
                },
                DisplayName = Path.GetFileName(project.FullPath),
                LanguageIds = new[] { LanguageId.Csharp },
                Capabilities = new BuildTargetCapabilities
                {
                    CanCompile = true,
                    CanRun = canRun,
                    CanDebug = canRun,
                    CanTest = canTest,
                },
                Tags = tags
            };
            list.Add(buildTarget);
        }

        return list;
    }
}
