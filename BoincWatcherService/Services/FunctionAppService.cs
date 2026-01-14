using BoincWatchService.Services.Interfaces;
using Common.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
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

	public async Task<bool> PutHostStats(HostStats hostStats, CancellationToken cancellationToken = default) {
		try {
			if (!_options.IsEnabled) {
				_logger.LogInformation("FunctionApp integration is disabled");
				return false;
			}

			var url = $"{_options.BaseUrl}/api/hoststats";
			var response = await _httpClient.PutAsJsonAsync(url, hostStats, cancellationToken);

			if (response.IsSuccessStatusCode) {
				_logger.LogInformation("Successfully uploaded host stats for {HostName}", hostStats.HostName);
				return true;
			} else {
				_logger.LogWarning("Failed to upload host stats for {HostName}. Status: {StatusCode}",
					hostStats.HostName, response.StatusCode);
				return false;
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error uploading host stats for {HostName}", hostStats.HostName);
			return false;
		}
	}

	public async Task<bool> PutProjectStats(ProjectStats projectStats, CancellationToken cancellationToken) {
		try {
			if (!_options.IsEnabled) {
				_logger.LogInformation("FunctionApp integration is disabled");
				return false;
			}

			var url = $"{_options.BaseUrl}/api/projectstats";
			var response = await _httpClient.PutAsJsonAsync(url, projectStats, cancellationToken);

			if (response.IsSuccessStatusCode) {
				_logger.LogInformation("Successfully uploaded project stats for {Project}", projectStats.ProjectName);
				return true;
			} else {
				_logger.LogWarning("Failed to upload project stats for {Project}. Status: {StatusCode}",
					projectStats.ProjectName, response.StatusCode);
				return false;
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error uploading project stats for {Project}", projectStats.ProjectName);
			return false;
		}
	}
}