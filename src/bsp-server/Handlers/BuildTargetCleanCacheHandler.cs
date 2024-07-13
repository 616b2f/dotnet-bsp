using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetCleanCache)]
internal class BuildTargetCleanCacheHandler
    : IRequestHandler<CleanCacheParams, CleanCacheResult, RequestContext>
{
    private readonly IInitializeManager<InitializeBuildParams, InitializeBuildResult> _capabilitiesManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildTargetCleanCacheHandler(
        IInitializeManager<InitializeBuildParams, InitializeBuildResult> capabilitiesManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _capabilitiesManager = capabilitiesManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => false;

    public Task<CleanCacheResult> HandleRequestAsync(CleanCacheParams cleanCacheParams, RequestContext context, CancellationToken cancellationToken)
    {
        var cleanResult = false;
        var initParams = _capabilitiesManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var projects = new ProjectCollection();
            foreach (var target in cleanCacheParams.Targets)
            {
                var fileExtension = Path.GetExtension(target.Uri.ToString());
                context.Logger.LogInformation("Target file extension {}", fileExtension);
                if (fileExtension == ".sln")
                {
                    var slnFile = SolutionFile.Parse(target.Uri.ToString());

                    var projectFilesInSln = slnFile.ProjectsInOrder
                        .Where(x => 
                            x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat ||
                            x.ProjectType == SolutionProjectType.WebProject)
                        .Select(x => x.AbsolutePath);

                    foreach (var projectFile in projectFilesInSln)
                    {
                        projects.LoadProject(projectFile);
                    }
                }
                else if (fileExtension == ".csproj")
                {
                    projects.LoadProject(target.Uri.ToString());
                }
            }

            var workspacePath = initParams.RootUri.AbsolutePath;
            context.Logger.LogInformation("GetLoadedProjects from {}", workspacePath);
            foreach (var proj in projects.LoadedProjects)
            {
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, null, workspacePath, proj.FullPath);
                context.Logger.LogInformation("Start clean for target: {}", proj.FullPath);
                cleanResult |= proj.Build("Clean", new [] { msBuildLogger });
            }
        }

        var message = cleanResult ? "Cleaned" : "Not cleaned";

        return Task.FromResult(new CleanCacheResult
        {
            Message = message,
            Cleaned = cleanResult
        });
    }
}