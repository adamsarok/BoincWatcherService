using BoincRpc;

namespace BoincWatcherService.Services.Interfaces;

public interface IRpcClientFactory
{
    RpcClient Create();
}
