using BaseProtocol;

namespace dotnet_bsp;

internal class RequestContextFactory : IRequestContextFactory<RequestContext>
{
    private readonly IBpServices _bpServices;

    public RequestContextFactory(IBpServices bpServices)
    {
        _bpServices = bpServices;
    }

    public Task<RequestContext> CreateRequestContextAsync<TRequestParam>(
        IQueueItem<RequestContext> queueItem,
        TRequestParam param,
        CancellationToken cancellationToken)
    {
        var logger = _bpServices.GetRequiredService<IBpLogger>();

        var requestContext = new RequestContext(_bpServices, logger);

        return Task.FromResult(requestContext);
    }
}

