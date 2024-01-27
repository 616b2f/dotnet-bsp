namespace BaseProtocol;

public class RequestContext
{
    public IBpServices BpServices;
    public IBpLogger Logger;

    public RequestContext(IBpServices bpServices, IBpLogger logger)
    {
        BpServices = bpServices;
        Logger = logger;
    }
}

