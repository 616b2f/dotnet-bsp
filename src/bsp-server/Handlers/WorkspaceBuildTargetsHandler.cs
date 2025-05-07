using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Newtonsoft.Json;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.WorkspaceBuildTargets)]
internal partial class WorkspaceBuildTargetsHandler
    : IRequestHandler<WorkspaceBuildTargetsResult, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager;
    public WorkspaceBuildTargetsHandler(BuildInitializeManager initializeManager)
    {
        _initializeManager = initializeManager;
    }

    public bool MutatesSolutionState => false;

    public Task<WorkspaceBuildTargetsResult> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialized();

        var list = new List<BuildTarget>();
        var initParams = _initializeManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var workspacePath = initParams.RootUri.LocalPath;
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
        logger.LogInformation("Search solution files in: {}", workspacePath);
        //TODO: think about the implications to search for all sln files in workspace
        var slnFilePath = Directory
            .GetFiles(workspacePath, "*.sln")
            .Take(1)
            .SingleOrDefault();
        var projectFiles = new List<string>();
        if (!string.IsNullOrWhiteSpace(slnFilePath))
        {
            logger.LogInformation("Found solution file: {}", slnFilePath);
            var solution = SolutionFile.Parse(slnFilePath);
            var dependencies = solution
                .GetProjects()
                .Select(x =>
                    new BuildTargetIdentifier
                    {
                        Uri = UriFixer.WithFileSchema(x.FullPath)
                    })
                .ToArray()
                .AsReadOnly();
            var buildTarget = new BuildTarget
            {
                Id = new BuildTargetIdentifier
                {
                    Uri = UriFixer.WithFileSchema(slnFilePath)
                },
                DisplayName = Path.GetFileName(slnFilePath),
                Dependencies = dependencies,
                LanguageIds = [LanguageId.Csharp],
                Capabilities = new BuildTargetCapabilities
                {
                    CanCompile = true,
                    CanTest = true,
                    CanDebug = false,
                    CanRun = false
                },
            };
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(slnFilePath));
            if (baseDirectory != null)
            {
                buildTarget.BaseDirectory = UriFixer.WithFileSchema(baseDirectory);
            }

            list.Add(buildTarget);

            var sln = SolutionFile.Parse(slnFilePath);
            var projectFilesInSln = sln.ProjectsInOrder
                .Where(x =>
                    x.ProjectType is
                        SolutionProjectType.KnownToBeMSBuildFormat or
                        SolutionProjectType.WebProject)
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

        // load targets from LaunchSettings
        var projectRootPaths = projects.Select(x => x.DirectoryPath);
        list.AddRange(GetLaunchSettingsProfilesAsBuildTargets(projectRootPaths, logger));

        return list;
    }

    private IEnumerable<BuildTarget> GetLaunchSettingsProfilesAsBuildTargets(IEnumerable<string> projectRootPaths, IBpLogger logger)
    {
        var list = new List<BuildTarget>();
        foreach(var projectRootPath in projectRootPaths)
        {
            var launchSettingsPath = Path.Combine(projectRootPath, "Properties", "launchSettings.json");
            if (File.Exists(launchSettingsPath))
            {
                var content = File.ReadAllText(launchSettingsPath);
                logger.LogInformation("launcSettings.json content: {}", content);
                var launchSettings = JsonConvert.DeserializeObject<LaunchSettings>(content);

                if (launchSettings is not null)
                {
                    logger.LogInformation(JsonConvert.SerializeObject(launchSettings));
                    foreach(var profile in launchSettings.Profiles)
                    {
                        list.Add(new BuildTarget
                        {
                            Id = new BuildTargetIdentifier
                            {
                                Uri = UriFixer.WithFileSchema($"{launchSettingsPath}#{profile.Key}")
                            },
                            DisplayName = profile.Key + " [LaunchProfile]",
                            LanguageIds = new[] { LanguageId.Csharp },
                            Capabilities = new BuildTargetCapabilities
                            {
                                CanRun = true,
                            },
                            Tags = [BuildTargetTag.Application]
                        });
                    }
                }
            }
        }
        return list;
    }

    private IEnumerable<BuildTarget> ProjectsToBuildTargets(IReadOnlyList<Project> projects)
    {
        var list = new List<BuildTarget>();
        foreach (var project in projects)
        {
            var canRun = project.IsRunnableProject();
            var canTest = project.IsTestProject();
            var tags = new List<BuildTargetTag>();
            if (canRun) {
                if (canTest)
                {
                    tags.Add(BuildTargetTag.Test);
                } else {
                    tags.Add(BuildTargetTag.Application);
                }
            }

            if (project.IsLibraryProject())
            {
                tags.Add(BuildTargetTag.Library);
            }

            var dependencies = project.GetProjectReferences()
                .Select(x => 
                    new BuildTargetIdentifier
                    {
                        Uri = UriFixer.WithFileSchema(x)
                    })
                .ToList()
                .AsReadOnly();

            var buildTarget = new BuildTarget
            {
                Id = new BuildTargetIdentifier
                {
                    Uri = UriFixer.WithFileSchema(project.FullPath)
                },
                DisplayName = Path.GetFileName(project.FullPath),
                LanguageIds = [LanguageId.Csharp],
                Dependencies = dependencies,
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