using Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatchService.Services.Interfaces;

public interface IFunctionAppService {
	Task<bool> PutHostStats(HostStatsTableEntity hostStats, CancellationToken cancellationToken);
	Task<bool> PutProjectStats(ProjectStatsTableEntity projectStats, CancellationToken cancellationToken);
}
