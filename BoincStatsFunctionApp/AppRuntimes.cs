using Azure.Data.Tables;
using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AdamSarok.BoincStatsFunctionApp;

public class AppRuntimes {
	private readonly ILogger<AppRuntimes> _logger;
	private readonly TableServiceClient _tableServiceClient;
	private const string TableName = "AppRuntimes";
	public AppRuntimes(ILogger<AppRuntimes> logger, TableServiceClient tableServiceClient) {
		_logger = logger;
		_tableServiceClient = tableServiceClient;
	}

	[Function("GetAppRuntime")]
	public async Task<IActionResult> GetHostStats(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "appruntimes/{partitionKey}/{rowKey}")] HttpRequest req,
		string partitionKey, string rowKey) {
		try {
			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();
			var entity = await tableClient.GetEntityAsync<AppRuntimeTableEntity>(partitionKey, rowKey);
			return new OkObjectResult(entity.Value);
		} catch (Azure.RequestFailedException ex) when (ex.Status == 404) {
			return new NotFoundResult();
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting app runtimes");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	[Function("GetAppRuntimes")]
	public async Task<IActionResult> GetAllHostStats(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "appruntimes")] HttpRequest req) {
		try {
			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();

			var query = tableClient.QueryAsync<AppRuntimeTableEntity>();

			var results = new List<AppRuntimeTableEntity>();
			await foreach (var entity in query) {
				results.Add(entity);
			}

			return new OkObjectResult(results);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting app runtimes");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	[Function("PutStats")]
	public async Task<IActionResult> PutStats(
		[HttpTrigger(AuthorizationLevel.Function, "put", Route = "appruntimes")] HttpRequest req) {
		try {
			var runtime = await JsonSerializer.DeserializeAsync<AppRuntimeTableEntity>(req.Body, new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			if (runtime == null || string.IsNullOrEmpty(runtime.PartitionKey) || string.IsNullOrEmpty(runtime.RowKey)) {
				return new BadRequestObjectResult("Invalid AppRuntimeTableEntity. PartitionKey and RowKey are required.");
			}

			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();

			await tableClient.UpsertEntityAsync(runtime);
			return new OkObjectResult(runtime);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error putting app runtimes");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

}
