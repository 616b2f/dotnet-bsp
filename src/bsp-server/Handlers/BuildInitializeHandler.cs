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

        // this has to be loaded before we try to use any Microsoft.Build.* references
        var msBuildInstance = MSBuildLocator.RegisterDefaults();
        context.Logger.LogInformation("MSBuild instance used: {}", msBuildInstance.MSBuildPath);

        var serverCapabilities = _initializeManager.GetInitializeResult();

        return Task.FromResult(serverCapabilities);
    }
}