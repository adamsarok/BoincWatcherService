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
	private const string TableName = "Stats";
	public Stats(ILogger<Stats> logger, TableServiceClient tableServiceClient) {
		_logger = logger;
		_tableServiceClient = tableServiceClient;
	}

	[Function("GetHostStats")]
	public async Task<IActionResult> GetHostStats(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "hoststats/{hostName}")] HttpRequest req,
		string hostName) {
		try {
			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();
			var entity = await tableClient.GetEntityAsync<StatsTableEntity>(StatsTableEntity.HOST_STATS, hostName);
			return new OkObjectResult(entity.Value);
		} catch (Azure.RequestFailedException ex) when (ex.Status == 404) {
			return new NotFoundResult();
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting HostStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	[Function("GetAllHostStats")]
	public async Task<IActionResult> GetAllHostStats(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "hoststats")] HttpRequest req) {
		try {
			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();

			var query = tableClient.QueryAsync<StatsTableEntity>(
				filter: $"PartitionKey eq '{StatsTableEntity.HOST_STATS}'");

			var results = new List<StatsTableEntity>();
			await foreach (var entity in query) {
				results.Add(entity);
			}

			return new OkObjectResult(results);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting HostStats for partition");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	[Function("PutStats")]
	public async Task<IActionResult> PutStats(
		[HttpTrigger(AuthorizationLevel.Function, "put", Route = "stats")] HttpRequest req) {
		try {
			var hostStats = await JsonSerializer.DeserializeAsync<StatsTableEntity>(req.Body, new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			if (hostStats == null || string.IsNullOrEmpty(hostStats.PartitionKey) || string.IsNullOrEmpty(hostStats.RowKey)) {
				return new BadRequestObjectResult("Invalid StatsTableEntity. PartitionKey and RowKey are required.");
			}

			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();

			await tableClient.UpsertEntityAsync(hostStats);
			return new OkObjectResult(hostStats);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error putting HostStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	[Function("GetProjectStats")]
	public async Task<IActionResult> GetProjectStats(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "projectstats/{projectName}")] HttpRequest req,
		string projectName) {
		try {
			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();

			var entity = await tableClient.GetEntityAsync<StatsTableEntity>(StatsTableEntity.PROJECT_STATS, projectName);
			return new OkObjectResult(entity.Value);
		} catch (Azure.RequestFailedException ex) when (ex.Status == 404) {
			return new NotFoundResult();
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting ProjectStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	[Function("GetAllProjectStats")]
	public async Task<IActionResult> GetAllProjectStats(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "projectstats")] HttpRequest req) {
		try {
			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();

			var query = tableClient.QueryAsync<StatsTableEntity>(
				filter: $"PartitionKey eq '{StatsTableEntity.PROJECT_STATS}'");

			var results = new List<StatsTableEntity>();
			await foreach (var entity in query) {
				results.Add(entity);
			}

			return new OkObjectResult(results);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting ProjectStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}
}
