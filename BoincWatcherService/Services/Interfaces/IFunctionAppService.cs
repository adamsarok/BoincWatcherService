using Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatchService.Services.Interfaces;

public interface IFunctionAppService {
	Task<bool> PutHostStats(HostStats hostStats, CancellationToken cancellationToken);
	Task<bool> PutProjectStats(ProjectStats projectStats, CancellationToken cancellationToken);
}
