
using StreamJsonRpc;

namespace dotnet_bsp
{
    [Serializable]
    internal class ServerNotInitializedException : LocalRpcException
    {
        public ServerNotInitializedException(string message): base(message: message)
        {
            ErrorCode = -32002;
        }
    }
}