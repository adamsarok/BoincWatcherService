using Common.Models;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatcherService.Services.Interfaces;

public interface IFunctionAppService {
	Task<bool> UploadStatsToFunctionApp(HttpClient httpClient, StatsTableEntity stats, CancellationToken cancellationToken);
	bool IsEnabled { get; }
}
