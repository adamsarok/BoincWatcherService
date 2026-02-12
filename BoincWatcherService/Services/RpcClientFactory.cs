using BoincRpc;
using BoincWatcherService.Services.Interfaces;

namespace BoincWatcherService.Services;

public class RpcClientFactory : IRpcClientFactory
{
    public RpcClient Create() => new RpcClient();
}
