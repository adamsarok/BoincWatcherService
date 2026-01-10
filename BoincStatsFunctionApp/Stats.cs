using Azure.Data.Tables;
using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AdamSarok.BoincStatsFunctionApp;

public class Stats {
	private readonly ILogger<Stats> _logger;
	private readonly TableServiceClient _tableServiceClient;

	public Stats(ILogger<Stats> logger, TableServiceClient tableServiceClient) {
		_logger = logger;
		_tableServiceClient = tableServiceClient;
	}


	// GET: /api/hoststats/{partitionKey}/{rowKey}
	[Function("GetHostStats")]
	public async Task<IActionResult> GetHostStats(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "hoststats/{partitionKey}/{rowKey}")] HttpRequest req,
		string partitionKey,
		string rowKey) {
		_logger.LogInformation($"Getting HostStats for PartitionKey: {partitionKey}, RowKey: {rowKey}");

		try {
			var tableClient = _tableServiceClient.GetTableClient("HostStats");
			await tableClient.CreateIfNotExistsAsync();

			var entity = await tableClient.GetEntityAsync<HostStatsTableEntity>(partitionKey, rowKey);
			return new OkObjectResult(entity.Value);
		} catch (Azure.RequestFailedException ex) when (ex.Status == 404) {
			return new NotFoundResult();
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting HostStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	// PUT: /api/hoststats
	[Function("PutHostStats")]
	public async Task<IActionResult> PutHostStats(
		[HttpTrigger(AuthorizationLevel.Function, "put", Route = "hoststats")] HttpRequest req) {
		_logger.LogInformation("Putting HostStats");

		try {
			var hostStats = await JsonSerializer.DeserializeAsync<HostStatsTableEntity>(req.Body, new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			if (hostStats == null || string.IsNullOrEmpty(hostStats.PartitionKey) || string.IsNullOrEmpty(hostStats.RowKey)) {
				return new BadRequestObjectResult("Invalid HostStatsDto. PartitionKey and RowKey are required.");
			}

			var tableClient = _tableServiceClient.GetTableClient("HostStats");
			await tableClient.CreateIfNotExistsAsync();

			await tableClient.UpsertEntityAsync(hostStats);
			return new OkObjectResult(hostStats);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error putting HostStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	// GET: /api/projectstats/{partitionKey}/{rowKey}
	[Function("GetProjectStats")]
	public async Task<IActionResult> GetProjectStats(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "projectstats/{partitionKey}/{rowKey}")] HttpRequest req,
		string partitionKey,
		string rowKey) {
		_logger.LogInformation($"Getting ProjectStats for PartitionKey: {partitionKey}, RowKey: {rowKey}");

		try {
			var tableClient = _tableServiceClient.GetTableClient("ProjectStats");
			await tableClient.CreateIfNotExistsAsync();

			var entity = await tableClient.GetEntityAsync<ProjectStatsTableEntity>(partitionKey, rowKey);
			return new OkObjectResult(entity.Value);
		} catch (Azure.RequestFailedException ex) when (ex.Status == 404) {
			return new NotFoundResult();
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting ProjectStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	// PUT: /api/projectstats
	[Function("PutProjectStats")]
	public async Task<IActionResult> PutProjectStats(
		[HttpTrigger(AuthorizationLevel.Function, "put", Route = "projectstats")] HttpRequest req) {
		_logger.LogInformation("Putting ProjectStats");

		try {
			var projectStats = await JsonSerializer.DeserializeAsync<ProjectStatsTableEntity>(req.Body, new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			if (projectStats == null || string.IsNullOrEmpty(projectStats.PartitionKey) || string.IsNullOrEmpty(projectStats.RowKey)) {
				return new BadRequestObjectResult("Invalid ProjectStatsTableEntity. PartitionKey and RowKey are required.");
			}

			var tableClient = _tableServiceClient.GetTableClient("ProjectStats");
			await tableClient.CreateIfNotExistsAsync();

			await tableClient.UpsertEntityAsync(projectStats);
			return new OkObjectResult(projectStats);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error putting ProjectStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}
}
