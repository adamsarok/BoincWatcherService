using BoincWatcherService.Models;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatchService.Services.Interfaces;

public interface IStatsService {
	Task<bool> UpsertHostStats(HostStats hostStats, CancellationToken cancellationToken = default);
	Task<bool> UpsertProjectStats(ProjectStats projectStats, CancellationToken cancellationToken = default);
	Task<bool> UpsertAggregateStats(CancellationToken cancellationToken = default);
}
