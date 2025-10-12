using System.Text.Json;
using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Locator;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildInitialize)]
internal class BuildInitializeHandler(BuildInitializeManager initializeManager)
    : IRequestHandler<InitializeBuildParams, InitializeBuildResult, RequestContext>
{
    private readonly BuildInitializeManager _initializeManager = initializeManager;

    public bool MutatesSolutionState => true;

    public Task<InitializeBuildResult> HandleRequestAsync(InitializeBuildParams request, RequestContext context, CancellationToken cancellationToken)
    {
        _initializeManager.SetInitializeParams(request);

        if (MSBuildLocator.IsRegistered)
        {
            throw new ServerNotInitializedException("MSBuild instance already registered.");
        }

        // this has to be loaded before we try to use any Microsoft.Build.* references
        var instanceQueryOptions = new VisualStudioInstanceQueryOptions
        {
            DiscoveryTypes = DiscoveryType.DotNetSdk,
            WorkingDirectory = request.RootUri.AbsolutePath,
        };

        var msBuildInstances = MSBuildLocator.QueryVisualStudioInstances(instanceQueryOptions)
            .OrderByDescending(x => x.Version);
        var msBuildInstance = msBuildInstances
            .FirstOrDefault();
        foreach (var instance in msBuildInstances)
        {
            context.Logger.LogInformation("MSBuild instance found: {}", JsonSerializer.Serialize(instance));
        }

        if (msBuildInstance == null)
        {
            throw new ServerNotInitializedException("MSBuild instance could not be found on your system.");
        }

        MSBuildLocator.RegisterInstance(msBuildInstance);
        context.Logger.LogInformation("MSBuild instance used: {}", JsonSerializer.Serialize(msBuildInstance));

        var serverCapabilities = _initializeManager.GetInitializeResult();

        return Task.FromResult(serverCapabilities);
    }
}