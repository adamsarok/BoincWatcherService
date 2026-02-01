using Azure.Data.Tables;
using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AdamSarok.BoincStatsFunctionApp;

public class Efficiency {
	private readonly ILogger<Efficiency> _logger;
	private readonly TableServiceClient _tableServiceClient;
	private const string TableName = "Efficiency";
	public Efficiency(ILogger<Efficiency> logger, TableServiceClient tableServiceClient) {
		_logger = logger;
		_tableServiceClient = tableServiceClient;
	}

	[Function("GetEfficiency")]
	public async Task<IActionResult> GetEfficiency(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "efficiency/{partitionKey}/{rowKey}")] HttpRequest req,
		string partitionKey, string rowKey) {
		try {
			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();
			var entity = await tableClient.GetEntityAsync<EfficiencyTableEntity>(partitionKey, rowKey);
			return new OkObjectResult(entity.Value);
		} catch (Azure.RequestFailedException ex) when (ex.Status == 404) {
			return new NotFoundResult();
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting efficiency");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	[Function("GetEfficiencies")]
	public async Task<IActionResult> GetAllEfficiencies(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "efficiency")] HttpRequest req) {
		try {
			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();

			var query = tableClient.QueryAsync<EfficiencyTableEntity>();

			var results = new List<EfficiencyTableEntity>();
			await foreach (var entity in query) {
				results.Add(entity);
			}

			return new OkObjectResult(results);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting efficiency");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	[Function("PutEfficiency")]
	public async Task<IActionResult> PutEfficiency(
		[HttpTrigger(AuthorizationLevel.Function, "put", Route = "efficiency")] HttpRequest req) {
		try {
			var efficiency = await JsonSerializer.DeserializeAsync<EfficiencyTableEntity>(req.Body, new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			if (efficiency == null || string.IsNullOrEmpty(efficiency.PartitionKey) || string.IsNullOrEmpty(efficiency.RowKey)) {
				return new BadRequestObjectResult("Invalid EfficiencyTableEntity. PartitionKey and RowKey are required.");
			}

			var tableClient = _tableServiceClient.GetTableClient(TableName);
			await tableClient.CreateIfNotExistsAsync();

			await tableClient.UpsertEntityAsync(efficiency);
			return new OkObjectResult(efficiency);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error putting efficiency");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

}
