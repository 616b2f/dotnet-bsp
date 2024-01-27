using BaseProtocol;
using bsp4csharp.Protocol;
using Microsoft.Build.Locator;

namespace dotnet_bsp.Handlers;

[BaseProtocolServerEndpoint(Methods.BuildInitialize)]
internal class BuildInitializeHandler
    : IRequestHandler<InitializeBuildParams, InitializeBuildResult, RequestContext>
{
    private readonly IInitializeManager<InitializeBuildParams, InitializeBuildResult> _capabilitiesManager;
    private readonly IBaseProtocolClientManager _baseProtocolClientManager;

    public BuildInitializeHandler(
        IInitializeManager<InitializeBuildParams, InitializeBuildResult> capabilitiesManager,
        IBaseProtocolClientManager baseProtocolClientManager)
    {
        _capabilitiesManager = capabilitiesManager;
        _baseProtocolClientManager = baseProtocolClientManager;
    }

    public bool MutatesSolutionState => true;

    public Task<InitializeBuildResult> HandleRequestAsync(InitializeBuildParams request, RequestContext context, CancellationToken cancellationToken)
    {
        _capabilitiesManager.SetInitializeParams(request);


        // this has to be loaded before we try to use any Microsoft.Build.* references
        var msBuildInstance = MSBuildLocator.RegisterDefaults();
        context.Logger.LogInformation("MSBuild instance used: {}", msBuildInstance.MSBuildPath);

        var serverCapabilities = _capabilitiesManager.GetInitializeResult();

        return Task.FromResult(serverCapabilities);
    }
}
