using Common.Models;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatcherService.Services.Interfaces;

public interface IFunctionAppService {
	Task<bool> UploadStatsToFunctionApp(HttpClient httpClient, StatsTableEntity stats, CancellationToken cancellationToken);
	Task<bool> UploadAppRuntimeToFunctionApp(HttpClient httpClient, AppRuntimeTableEntity runtime, CancellationToken cancellationToken);
	Task<bool> UploadEfficiencyToFunctionApp(HttpClient httpClient, EfficiencyTableEntity efficiency, CancellationToken cancellationToken);
	bool IsEnabled { get; }
}
