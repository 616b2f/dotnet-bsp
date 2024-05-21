using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using dotnet_bsp.Logging;

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
        foreach (var target in compileParams.Targets)
        {
            var fileExtension = Path.GetExtension(target.Uri.ToString());
            context.Logger.LogInformation("Target file extension {}", fileExtension);
            if (fileExtension == ".sln")
            {
                var slnFile = SolutionFile.Parse(target.Uri.ToString());

                var configurationName = slnFile.GetDefaultConfigurationName();
                var platformName = slnFile.GetDefaultPlatformName();
                if (string.Equals(platformName, "Any CPU", StringComparison.InvariantCultureIgnoreCase))
                {
                    platformName = "AnyCpu";
                }

                context.Logger.LogInformation("use platformName: {}", platformName);
                context.Logger.LogInformation("use configurationName: {}", configurationName);
                var projectFilesInSln = slnFile.ProjectsInOrder
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
                var msBuildLogger = new MSBuildLogger(_baseProtocolClientManager, workspacePath);
                buildResult = proj.Build("Restore", new [] {msBuildLogger});
                buildResult &= proj.Build("Build", new [] {msBuildLogger});
            }
        }

        return Task.FromResult(new CompileResult
        {
            StatusCode = buildResult ? StatusCode.Ok : StatusCode.Error
        });
    }
}
