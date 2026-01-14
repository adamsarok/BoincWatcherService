using AdamSarok.BoincStatsFunctionApp.Data;
using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AdamSarok.BoincStatsFunctionApp;

public class Stats {
	private readonly ILogger<Stats> _logger;
	private readonly StatsDbContext _dbContext;

	public Stats(ILogger<Stats> logger, StatsDbContext dbContext) {
		_logger = logger;
		_dbContext = dbContext;
	}


	// GET: /api/hoststats/{partitionKey}/{rowKey}
	[Function("GetHostStats")]
	public async Task<IActionResult> GetHostStats(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "hoststats/{partitionKey}/{rowKey}")] HttpRequest req,
		string partitionKey,
		string rowKey) {
		try {
			var entity = await _dbContext.HostStats
				.FirstOrDefaultAsync(h => h.YYYYMMDD == partitionKey && h.HostName == rowKey);

			if (entity == null) {
				return new NotFoundResult();
			}

			return new OkObjectResult(entity);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting HostStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	// GET: /api/projectstats/{partitionKey}/
	[Function("GetAllHostStatsForDate")]
	public async Task<IActionResult> GetAllHostStatsForDate(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "hoststats/{partitionKey}")] HttpRequest req,
		string partitionKey) {
		try {
			var results = await _dbContext.HostStats
				.Where(h => h.YYYYMMDD == partitionKey)
				.ToListAsync();

			return new OkObjectResult(results);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting HostStats for partition");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	// PUT: /api/hoststats
	[Function("PutHostStats")]
	public async Task<IActionResult> PutHostStats(
		[HttpTrigger(AuthorizationLevel.Function, "put", Route = "hoststats")] HttpRequest req) {
		try {
			var hostStats = await JsonSerializer.DeserializeAsync<HostStats>(req.Body, new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			if (hostStats == null || string.IsNullOrEmpty(hostStats.YYYYMMDD) || string.IsNullOrEmpty(hostStats.HostName)) {
				return new BadRequestObjectResult("Invalid HostStats. YYMMMDDDD and HostName are required.");
			}

			// Check if entity exists
			var existingEntity = await _dbContext.HostStats
				.FirstOrDefaultAsync(h => h.YYYYMMDD == hostStats.YYYYMMDD && h.HostName == hostStats.HostName);

			if (existingEntity != null) {
				// Update existing entity
				existingEntity.TotalCredit = hostStats.TotalCredit;
				existingEntity.Timestamp = hostStats.Timestamp ?? DateTimeOffset.UtcNow;
				existingEntity.LatestTaskDownloadTime = hostStats.LatestTaskDownloadTime;
				_dbContext.HostStats.Update(existingEntity);
			} else {
				// Insert new entity
				hostStats.Timestamp = hostStats.Timestamp ?? DateTimeOffset.UtcNow;
				_dbContext.HostStats.Add(hostStats);
			}

			await _dbContext.SaveChangesAsync();
			return new OkObjectResult(existingEntity ?? hostStats);
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
		try {
			var entity = await _dbContext.ProjectStats
				.FirstOrDefaultAsync(p => p.YYYYMMDD == partitionKey && p.ProjectName == rowKey);

			if (entity == null) {
				return new NotFoundResult();
			}

			return new OkObjectResult(entity);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting ProjectStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	// GET: /api/projectstats/{partitionKey}/
	[Function("GetAllProjectStatsForDate")]
	public async Task<IActionResult> GetAllProjectStatsForDate(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "projectstats/{partitionKey}")] HttpRequest req,
		string partitionKey) {
		try {
			var results = await _dbContext.ProjectStats
				.Where(p => p.YYYYMMDD == partitionKey)
				.ToListAsync();

			return new OkObjectResult(results);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error getting ProjectStats for partition");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	// PUT: /api/projectstats
	[Function("PutProjectStats")]
	public async Task<IActionResult> PutProjectStats(
		[HttpTrigger(AuthorizationLevel.Function, "put", Route = "projectstats")] HttpRequest req) {
		try {
			var projectStats = await JsonSerializer.DeserializeAsync<ProjectStats>(req.Body, new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			if (projectStats == null || string.IsNullOrEmpty(projectStats.YYYYMMDD) || string.IsNullOrEmpty(projectStats.ProjectName)) {
				return new BadRequestObjectResult("Invalid ProjectStats. YYYYMMDD and ProjectName are required.");
			}

			// Check if entity exists
			var existingEntity = await _dbContext.ProjectStats
				.FirstOrDefaultAsync(p => p.YYYYMMDD == projectStats.YYYYMMDD && p.ProjectName == projectStats.ProjectName);

			if (existingEntity != null) {
				// Update existing entity
				existingEntity.TotalCredit = projectStats.TotalCredit;
				existingEntity.Timestamp = projectStats.Timestamp ?? DateTimeOffset.UtcNow;
				existingEntity.LatestTaskDownloadTime = projectStats.LatestTaskDownloadTime;
				_dbContext.ProjectStats.Update(existingEntity);
			} else {
				// Insert new entity
				projectStats.Timestamp = projectStats.Timestamp ?? DateTimeOffset.UtcNow;
				_dbContext.ProjectStats.Add(projectStats);
			}

			await _dbContext.SaveChangesAsync();
			return new OkObjectResult(existingEntity ?? projectStats);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error putting ProjectStats");
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}
}
