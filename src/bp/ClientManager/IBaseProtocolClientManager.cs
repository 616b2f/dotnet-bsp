using System.Threading;
using System.Threading.Tasks;

namespace BaseProtocol
{
    public interface IBaseProtocolClientManager : IBpService
    {
        Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken);
        ValueTask SendRequestAsync(string methodName, CancellationToken cancellationToken);
        ValueTask SendRequestAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken);
        ValueTask SendNotificationAsync(string methodName, CancellationToken cancellationToken);
        ValueTask SendNotificationAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken);
    }
}
