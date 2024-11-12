using BaseProtocol;
using BaseProtocol.ClientManager;
using BaseProtocol.Handlers;
using BaseProtocol.Server;
using bsp4csharp.Protocol;
using dotnet_bsp.Handlers;
using Microsoft.Extensions.DependencyInjection;
using StreamJsonRpc;

namespace dotnet_bsp;

public class BuildServer : AbstractBaseProtocolServer<RequestContext>
{
    private readonly JsonRpc _jsonRpc;
    private readonly Action<IServiceCollection>? _addExtraHandlers;

    public BuildServer(
        JsonRpc jsonRpc,
        IBpLogger logger,
        Action<IServiceCollection>? addExtraHandlers) : base(jsonRpc, logger)
    {
        this._jsonRpc = jsonRpc;
        _addExtraHandlers = addExtraHandlers;
        // This spins up the queue and ensure the BP server is ready to start receiving requests
        Initialize();
    }

    protected override IHandlerProvider GetHandlerProvider()
    {
        var bpServices = GetBpServices();
        var handlerProvider = new HandlerProvider(bpServices,
            new List<string>{
                Methods.BuildInitialize,
                Methods.BuildInitialized,
                Methods.BuildExit,
                Methods.BuildShutdown,
            });
        SetupRequestDispatcher(handlerProvider);

        return handlerProvider;
    }

    protected override IBpServices ConstructBpServices()
    {
        var baseProtocolClientManager = new BaseProtocolClientManager(_jsonRpc);
        var serviceCollection = new ServiceCollection();

        var _ = AddHandlers(serviceCollection)
            .AddSingleton<IBaseProtocolClientManager>(baseProtocolClientManager)
            .AddSingleton<IBpLogger>(_logger)
            .AddSingleton<IRequestContextFactory<RequestContext>, RequestContextFactory>()
            .AddSingleton<IHandlerProvider>(s => GetHandlerProvider())
            .AddSingleton<BuildInitializeManager>()
            // .AddSingleton<IInitializeManager<InitializeBuildParams, InitializeBuildResult>, BuildInitializeManager>()
            .AddSingleton<ILifeCycleManager, BpServiceLifeCycleManager>()
            .AddSingleton(this);

        var bpServices = new BpServices(serviceCollection);

        return bpServices;
    }

    [JsonRpcMethod(Methods.BuildShutdown)]
    public Task HandleBuildShutdownRequestAsync(CancellationToken _) => ShutdownAsync();

    [JsonRpcMethod(Methods.BuildExit)]
    public Task HandleBuildExitNotificationAsync(CancellationToken _) => ExitAsync();

    protected virtual IServiceCollection AddHandlers(IServiceCollection serviceCollection)
    {
        _ = serviceCollection
            .AddSingleton<IMethodHandler, BuildInitializeHandler>()
            .AddSingleton<IMethodHandler, BuildInitializedHandler>()
            .AddSingleton<IMethodHandler, BuildTargetSourcesHandler>()
            .AddSingleton<IMethodHandler, BuildTargetCompileHandler>()
            .AddSingleton<IMethodHandler, BuildTargetRunHandler>()
            .AddSingleton<IMethodHandler, BuildTargetTestHandler>()
            .AddSingleton<IMethodHandler, BuildTargetCleanCacheHandler>()
            .AddSingleton<IMethodHandler, BuildTargetTestCaseDiscoveryHandler>()
            .AddSingleton<IMethodHandler, WorkspaceBuildTargetsHandler>();

        if (_addExtraHandlers is not null)
        {
            _addExtraHandlers(serviceCollection);
        }

        return serviceCollection;
    }
}