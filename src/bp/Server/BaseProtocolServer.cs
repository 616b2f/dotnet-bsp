using System;
using BaseProtocol.Protocol;
using StreamJsonRpc;
using Microsoft.Extensions.DependencyInjection;
using BaseProtocol.Handlers;
using BaseProtocol.ClientManager;

namespace BaseProtocol.Server;

internal class BaseProtocolServer : AbstractBaseProtocolServer<RequestContext>
{
    private readonly JsonRpc _jsonRpc;
    private readonly IRequestContextFactory<RequestContext> _requestContextFactory;
    private readonly IInitializeManager<InitializeParams, InitializeResult> _initializeManager;
    private readonly Action<IServiceCollection>? _addExtraHandlers;

    internal BaseProtocolServer(
        JsonRpc jsonRpc,
        IRequestContextFactory<RequestContext> requestContextFactory,
        IInitializeManager<InitializeParams, InitializeResult> initializeManager,
        IBpLogger logger,
        Action<IServiceCollection>? addExtraHandlers) : base(jsonRpc, logger)
    {
        _jsonRpc = jsonRpc;
        _requestContextFactory = requestContextFactory;
        _initializeManager = initializeManager;
        _addExtraHandlers = addExtraHandlers;
        // This spins up the queue and ensure the BP server is ready to start receiving requests
        Initialize();
    }

    protected override IBpServices ConstructBpServices()
    {
        var baseProtocolClientManager = new BaseProtocolClientManager(_jsonRpc);
        var serviceCollection = new ServiceCollection();

        var _ = AddHandlers(serviceCollection)
            .AddSingleton<IBpLogger>(_logger)
            .AddSingleton<IRequestContextFactory<RequestContext>>(_requestContextFactory)
            .AddSingleton<IHandlerProvider>(s => GetHandlerProvider())
            .AddSingleton<IInitializeManager<InitializeParams, InitializeResult>>(_initializeManager)
            .AddSingleton<ILifeCycleManager>(new BpServiceLifeCycleManager(baseProtocolClientManager))
            .AddSingleton(this);

        var lifeCycleManager = GetLifeCycleManager();
        if (lifeCycleManager != null)
        {
            serviceCollection.AddSingleton(lifeCycleManager);
        }

        var bpServices = new BpServices(serviceCollection);

        return bpServices;
    }

    protected virtual ILifeCycleManager? GetLifeCycleManager()
    {
        return null;
    }

    protected virtual IServiceCollection AddHandlers(IServiceCollection serviceCollection)
    {
        _ = serviceCollection
            .AddSingleton<IMethodHandler, InitializeHandler<InitializeParams, InitializeResult, RequestContext>>()
            .AddSingleton<IMethodHandler, InitializedHandler<InitializedParams, RequestContext>>();

        if (_addExtraHandlers is not null)
        {
            _addExtraHandlers(serviceCollection);
        }

        return serviceCollection;
    }
}
