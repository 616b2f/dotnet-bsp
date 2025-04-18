using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetCleanCache)]
internal class BuildTargetCleanCacheHandler(
    BuildInitializeManager initializeManager,
    IBaseProtocolClientManager baseProtocolClientManager)
        : IRequestHandler<CleanCacheParams, CleanCacheResult, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager = initializeManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager = baseProtocolClientManager;

    public bool MutatesSolutionState => false;

    public Task<CleanCacheResult> HandleRequestAsync(CleanCacheParams cleanCacheParams, RequestContext context, CancellationToken cancellationToken)
    {
        _initializeManager.EnsureInitialized();

        var cleanResult = false;
        var initParams = _initializeManager.GetInitializeParams();
        if (initParams.RootUri.IsFile)
        {
            var projects = new ProjectCollection();
            foreach (var target in cleanCacheParams.Targets)
            {
                var fileExtension = Path.GetExtension(target.ToString());
                context.Logger.LogInformation("Target file extension {}", fileExtension);
                if (fileExtension == ".sln")
                {
                    var slnFile = SolutionFile.Parse(target.ToString());

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
                    projects.LoadProject(target.ToString());
                }
            }

            var workspacePath = initParams.RootUri.LocalPath;
            context.Logger.LogInformation("GetLoadedProjects from {}", workspacePath);
            foreach (var proj in projects.LoadedProjects)
            {
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, null, workspacePath);
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