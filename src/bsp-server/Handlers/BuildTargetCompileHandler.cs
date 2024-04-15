using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis.MSBuild;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildTargetCompile)]
internal class BuildTargetCompileHandler
    : IRequestHandler<CompileParams, CompileResult, RequestContext>
{
    private readonly IInitializeManager<InitializeBuildParams, InitializeBuildResult> _capabilitiesManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildTargetCompileHandler(
        IInitializeManager<InitializeBuildParams, InitializeBuildResult> capabilitiesManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _capabilitiesManager = capabilitiesManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => false;

    public Task<CompileResult> HandleRequestAsync(CompileParams compileParams, RequestContext context, CancellationToken cancellationToken)
    {
        var projects = new ProjectCollection();
        var buildResult = false;
        using (var workspace = MSBuildWorkspace.Create())
        {
            foreach (var target in compileParams.Targets)
            {
                var fileExtension = Path.GetExtension(target.Uri.ToString());
                context.Logger.LogInformation("Target file extension {}", fileExtension);
                if (fileExtension == ".sln")
                {
                    var slnFile = SolutionFile.Parse(target.Uri.ToString());
                    var sln = workspace.OpenSolutionAsync(target.Uri.ToString()).GetAwaiter().GetResult();
                    var projectIds = sln.GetProjectDependencyGraph().GetTopologicallySortedProjects();

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

            var initParams = _capabilitiesManager.GetInitializeParams();
            if (initParams.RootUri.IsFile)
            {
                var workspacePath = initParams.RootUri.AbsolutePath;
                context.Logger.LogInformation("GetLoadedProjects from {}", workspacePath);
                foreach (var proj in projects.LoadedProjects)
                {
                    context.Logger.LogInformation("Start building target: {}", proj.ProjectFileLocation);
                    var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager);
                    buildResult = proj.Build(msBuildLogger);
                }
            }
        }

        return Task.FromResult(new CompileResult
        {
            StatusCode = buildResult ? StatusCode.Ok : StatusCode.Error
        });
    }
}

