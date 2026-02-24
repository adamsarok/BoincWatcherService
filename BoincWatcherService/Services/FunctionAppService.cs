using BoincWatcherService.Services.Interfaces;
using Common.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatcherService.Services;

public class FunctionAppService(
	IOptions<FunctionAppOptions> functionAppOptions,
	ILogger<FunctionAppService> logger) : IFunctionAppService {

	public async Task<bool> UploadAppRuntimeToFunctionApp(HttpClient httpClient, AppRuntimeTableEntity entity, CancellationToken cancellationToken) {
		if (functionAppOptions is null) {
			throw new ArgumentNullException(nameof(functionAppOptions));
		}
		if (string.IsNullOrEmpty(functionAppOptions.Value.BaseUrl)) {
			throw new InvalidOperationException("BaseUrl is not configured.");
		}
		try {
			var url = $"{functionAppOptions.Value.BaseUrl.TrimEnd('/')}/api/appruntimes";
			using var request = new HttpRequestMessage(HttpMethod.Put, url) {
				Content = JsonContent.Create(entity)
			};

			if (!string.IsNullOrEmpty(functionAppOptions.Value.FunctionKey)) {
				request.Headers.Add("x-functions-key", functionAppOptions.Value.FunctionKey);
			}

			using var response = await httpClient.SendAsync(request, cancellationToken);

			if (response.IsSuccessStatusCode) {
				logger.LogDebug("Successfully uploaded app runtimes for {PartitionKey}/{RowKey}", entity.PartitionKey, entity.RowKey);
				return true;
			} else {
				logger.LogWarning("Failed to upload app runtimes for {PartitionKey}/{RowKey}. Status: {StatusCode}",
					entity.PartitionKey, entity.RowKey, response.StatusCode);
				return false;
			}
		} catch (Exception ex) {
			logger.LogError(ex, "Error uploading app runtimes for {PartitionKey}/{RowKey}", entity.PartitionKey, entity.RowKey);
			return false;
		}
	}

	public async Task<bool> UploadStatsToFunctionApp(HttpClient httpClient, StatsTableEntity stats, CancellationToken cancellationToken) {
		if (functionAppOptions is null) {
			throw new ArgumentNullException(nameof(functionAppOptions));
		}
		if (string.IsNullOrEmpty(functionAppOptions.Value.BaseUrl)) {
			throw new InvalidOperationException("BaseUrl is not configured.");
		}
		try {
			var url = $"{functionAppOptions.Value.BaseUrl.TrimEnd('/')}/api/stats";
			using var request = new HttpRequestMessage(HttpMethod.Put, url) {
				Content = JsonContent.Create(stats)
			};

			if (!string.IsNullOrEmpty(functionAppOptions.Value.FunctionKey)) {
				request.Headers.Add("x-functions-key", functionAppOptions.Value.FunctionKey);
			}

			using var response = await httpClient.SendAsync(request, cancellationToken);

			if (response.IsSuccessStatusCode) {
				logger.LogDebug("Successfully uploaded stats for {PartitionKey}/{RowKey}", stats.PartitionKey, stats.RowKey);
				return true;
			} else {
				logger.LogWarning("Failed to upload stats for {PartitionKey}/{RowKey}. Status: {StatusCode}",
					stats.PartitionKey, stats.RowKey, response.StatusCode);
				return false;
			}
		} catch (Exception ex) {
			logger.LogError(ex, "Error uploading stats for {PartitionKey}/{RowKey}", stats.PartitionKey, stats.RowKey);
			return false;
		}
	}

	public async Task<bool> UploadEfficiencyToFunctionApp(HttpClient httpClient, EfficiencyTableEntity efficiency, CancellationToken cancellationToken) {
		if (functionAppOptions is null) {
			throw new ArgumentNullException(nameof(functionAppOptions));
		}
		if (string.IsNullOrEmpty(functionAppOptions.Value.BaseUrl)) {
			throw new InvalidOperationException("BaseUrl is not configured.");
		}
		try {
			var url = $"{functionAppOptions.Value.BaseUrl.TrimEnd('/')}/api/efficiency";
			using var request = new HttpRequestMessage(HttpMethod.Put, url) {
				Content = JsonContent.Create(efficiency)
			};

			if (!string.IsNullOrEmpty(functionAppOptions.Value.FunctionKey)) {
				request.Headers.Add("x-functions-key", functionAppOptions.Value.FunctionKey);
			}

			using var response = await httpClient.SendAsync(request, cancellationToken);

			if (response.IsSuccessStatusCode) {
				logger.LogDebug("Successfully uploaded efficiency for {PartitionKey}/{RowKey}", efficiency.PartitionKey, efficiency.RowKey);
				return true;
			} else {
				logger.LogWarning("Failed to upload efficiency for {PartitionKey}/{RowKey}. Status: {StatusCode}",
					efficiency.PartitionKey, efficiency.RowKey, response.StatusCode);
				return false;
			}
		} catch (Exception ex) {
			logger.LogError(ex, "Error uploading efficiency for {PartitionKey}/{RowKey}", efficiency.PartitionKey, efficiency.RowKey);
			return false;
		}
	}
}
