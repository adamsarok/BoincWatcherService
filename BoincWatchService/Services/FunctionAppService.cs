using BoincWatchService.DTO;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BoincWatchService.Services;

public class FunctionAppService : IFunctionAppService {
	private readonly ILogger<FunctionAppService> _logger;
	private readonly HttpClient _httpClient;
	private readonly FunctionAppOptions _options;

	public FunctionAppService(ILogger<FunctionAppService> logger, HttpClient httpClient, IOptions<FunctionAppOptions> options) {
		_logger = logger;
		_httpClient = httpClient;
		_options = options.Value;

		if (!string.IsNullOrEmpty(_options.FunctionKey)) {
			_httpClient.DefaultRequestHeaders.Add("x-functions-key", _options.FunctionKey);
		}
	}

	public async Task<bool> PutHostStats(HostStatsDto hostStats, CancellationToken cancellationToken = default) {
		try {
			if (!_options.IsEnabled) {
				_logger.LogInformation("FunctionApp integration is disabled");
				return false;
			}

			var url = $"{_options.BaseUrl}/api/hoststats";
			var response = await _httpClient.PutAsJsonAsync(url, hostStats, cancellationToken);

			if (response.IsSuccessStatusCode) {
				_logger.LogInformation("Successfully uploaded host stats for {HostName}", hostStats.RowKey);
				return true;
			} else {
				_logger.LogWarning("Failed to upload host stats for {HostName}. Status: {StatusCode}",
					hostStats.RowKey, response.StatusCode);
				return false;
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error uploading host stats for {HostName}", hostStats.RowKey);
			return false;
		}
	}
}

public interface IFunctionAppService {
	Task<bool> PutHostStats(HostStatsDto hostStats);
}
