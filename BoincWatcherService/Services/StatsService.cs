using BoincWatcherService.Models;
using BoincWatchService.Data;
using BoincWatchService.Services.Interfaces;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatchService.Services;

public class StatsService : IStatsService {
	private readonly ILogger<StatsService> _logger;
	private readonly StatsDbContext _dbContext;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly FunctionAppOptions _functionAppOptions;

	public StatsService(
	ILogger<StatsService> logger,
	StatsDbContext dbContext,
	IHttpClientFactory httpClientFactory,
	IOptions<FunctionAppOptions> functionAppOptions) {
		_logger = logger;
		_dbContext = dbContext;
		_httpClientFactory = httpClientFactory;
		_functionAppOptions = functionAppOptions.Value;
	}

	public async Task<bool> UpsertHostStats(HostStats hostStats, CancellationToken cancellationToken = default) {
		try {
			if (string.IsNullOrEmpty(hostStats.YYYYMMDD) || string.IsNullOrEmpty(hostStats.HostName)) {
				_logger.LogWarning("Invalid HostStats. YYYYMMDD and HostName are required.");
				return false;
			}

			var existingEntity = await _dbContext.HostStats
				.FirstOrDefaultAsync(h => h.YYYYMMDD == hostStats.YYYYMMDD && h.HostName == hostStats.HostName, cancellationToken);

			if (existingEntity != null) {
				existingEntity.TotalCredit = hostStats.TotalCredit;
				existingEntity.Timestamp = hostStats.Timestamp ?? DateTimeOffset.UtcNow;
				existingEntity.LatestTaskDownloadTime = hostStats.LatestTaskDownloadTime;
				_dbContext.HostStats.Update(existingEntity);
			} else {
				hostStats.Timestamp = hostStats.Timestamp ?? DateTimeOffset.UtcNow;
				_dbContext.HostStats.Add(hostStats);
			}

			await _dbContext.SaveChangesAsync(cancellationToken);
			_logger.LogInformation("Successfully saved host stats for {HostName}", hostStats.HostName);
			return true;
		} catch (Exception ex) {
			_logger.LogError(ex, "Error saving host stats for {HostName}", hostStats.HostName);
			return false;
		}
	}

	public async Task<bool> UpsertProjectStats(ProjectStats projectStats, CancellationToken cancellationToken = default) {
		try {
			if (string.IsNullOrEmpty(projectStats.YYYYMMDD) || string.IsNullOrEmpty(projectStats.ProjectName)) {
				_logger.LogWarning("Invalid ProjectStats. YYYYMMDD and ProjectName are required.");
				return false;
			}

			var existingEntity = await _dbContext.ProjectStats
				.FirstOrDefaultAsync(p => p.YYYYMMDD == projectStats.YYYYMMDD && p.ProjectName == projectStats.ProjectName, cancellationToken);

			if (existingEntity != null) {
				existingEntity.TotalCredit = projectStats.TotalCredit;
				existingEntity.Timestamp = projectStats.Timestamp ?? DateTimeOffset.UtcNow;
				existingEntity.LatestTaskDownloadTime = projectStats.LatestTaskDownloadTime;
				_dbContext.ProjectStats.Update(existingEntity);
			} else {
				projectStats.Timestamp = projectStats.Timestamp ?? DateTimeOffset.UtcNow;
				_dbContext.ProjectStats.Add(projectStats);
			}

			await _dbContext.SaveChangesAsync(cancellationToken);
			_logger.LogInformation("Successfully saved project stats for {ProjectName}", projectStats.ProjectName);
			return true;
		} catch (Exception ex) {
			_logger.LogError(ex, "Error saving project stats for {ProjectName}", projectStats.ProjectName);
			return false;
		}
	}

	public async Task<bool> UpsertAggregateStats(CancellationToken cancellationToken = default) {
		try {
			if (!_functionAppOptions.IsEnabled) {
				_logger.LogWarning("Function app is not enabled. Skipping aggregate stats upload.");
				return false;
			}

			if (string.IsNullOrEmpty(_functionAppOptions.BaseUrl)) {
				_logger.LogWarning("Function app base URL is not configured.");
				return false;
			}

			var today = DateTime.UtcNow.Date;

			// Aggregate HostStats
			var hostStatsAggregated = await AggregateHostStats(today, cancellationToken);

			// Aggregate ProjectStats
			var projectStatsAggregated = await AggregateProjectStats(today, cancellationToken);

			// Upload to function app
			var httpClient = _httpClientFactory.CreateClient();
			var success = true;

			foreach (var hostStat in hostStatsAggregated) {
				if (!await UploadStatsToFunctionApp(httpClient, hostStat, cancellationToken)) {
					success = false;
				}
			}

			foreach (var projectStat in projectStatsAggregated) {
				if (!await UploadStatsToFunctionApp(httpClient, projectStat, cancellationToken)) {
					success = false;
				}
			}

			if (success) {
				_logger.LogInformation("Successfully uploaded aggregate stats for {hostCount} hosts and {projectCount} projects",
					hostStatsAggregated.Count, projectStatsAggregated.Count);
			}

			return success;
		} catch (Exception ex) {
			_logger.LogError(ex, "Error upserting aggregate stats");
			return false;
		}
	}

	private async Task<List<StatsTableEntity>> AggregateHostStats(
		DateTime today,
		CancellationToken cancellationToken) {

		var yesterday = today.AddDays(-1);
		var weekStart = today.AddDays(-(int)today.DayOfWeek);
		var monthStart = new DateTime(today.Year, today.Month, 1);
		var yearStart = new DateTime(today.Year, 1, 1);

		// Get latest stats for each project using raw SQL
		var latestStats = await _dbContext.HostStats
			.FromSqlRaw(@"
				SELECT p.*
				FROM ""HostStats"" p
				INNER JOIN (
					SELECT ""HostName"", MAX(""YYYYMMDD"") as max_yyyymmdd
					FROM ""HostStats""
					GROUP BY ""HostName""
				) latest
				ON p.""HostName"" = latest.""HostName""
				AND p.""YYYYMMDD"" = latest.max_yyyymmdd")
			.AsNoTracking()
			.ToDictionaryAsync(c => c.HostName, v => new StatsTableEntity() {
				PartitionKey = StatsTableEntity.HOST_STATS,
				RowKey = v.HostName,
				CreditTotal = v.TotalCredit,
				LatestTaskDownloadTime = v.LatestTaskDownloadTime
			}, cancellationToken);

		var yesterdayStats = await GetEarliestHostStatAfter(yesterday, cancellationToken);
		foreach (var stat in yesterdayStats) {
			latestStats.TryGetValue(stat.HostName, out var aggregated);
			if (aggregated != null) aggregated.CreditToday = aggregated.CreditTotal - stat.TotalCredit;
		}

		var weekStartStats = await GetEarliestHostStatAfter(weekStart, cancellationToken);
		foreach (var stat in weekStartStats) {
			latestStats.TryGetValue(stat.HostName, out var aggregated);
			if (aggregated != null) aggregated.CreditThisWeek = aggregated.CreditTotal - stat.TotalCredit;
		}

		var monthStartStats = await GetEarliestHostStatAfter(monthStart, cancellationToken);
		foreach (var stat in monthStartStats) {
			latestStats.TryGetValue(stat.HostName, out var aggregated);
			if (aggregated != null) aggregated.CreditThisMonth = aggregated.CreditTotal - stat.TotalCredit;
		}

		var yearStartStats = await GetEarliestHostStatAfter(yearStart, cancellationToken);
		foreach (var stat in yearStartStats) {
			latestStats.TryGetValue(stat.HostName, out var aggregated);
			if (aggregated != null) aggregated.CreditThisYear = aggregated.CreditTotal - stat.TotalCredit;
		}

		return latestStats.Values.ToList();
	}
	private async Task<List<(string HostName, double TotalCredit)>> GetEarliestHostStatAfter(DateTime fromDate,
	CancellationToken cancellationToken) {
		var results = await _dbContext.HostStats
			.FromSqlRaw(@"
				SELECT p.*
				FROM ""HostStats"" p
				INNER JOIN (
					SELECT ""HostName"", MIN(""YYYYMMDD"") as min_yyyymmdd
					FROM ""HostStats""
					WHERE ""YYYYMMDD"" >= {0}
					GROUP BY ""HostName""
				) earliest
				ON p.""HostName"" = earliest.""HostName""
				AND p.""YYYYMMDD"" = earliest.min_yyyymmdd", fromDate.ToString("yyyyMMdd"))
			.AsNoTracking()
			.Select(x => new { x.HostName, x.TotalCredit })
			.ToListAsync(cancellationToken);

		return results.Select(x => (x.HostName, x.TotalCredit)).ToList();
	}
	private async Task<List<(string ProjectName, double TotalCredit)>> GetEarliestProjectStatAfter(DateTime fromDate,
		CancellationToken cancellationToken) {
		var results = await _dbContext.ProjectStats
			.FromSqlRaw(@"
				SELECT p.*
				FROM ""ProjectStats"" p
				INNER JOIN (
					SELECT ""ProjectName"", MIN(""YYYYMMDD"") as min_yyyymmdd
					FROM ""ProjectStats""
					WHERE ""YYYYMMDD"" >= {0}
					GROUP BY ""ProjectName""
				) earliest
				ON p.""ProjectName"" = earliest.""ProjectName""
				AND p.""YYYYMMDD"" = earliest.min_yyyymmdd", fromDate.ToString("yyyyMMdd"))
			.AsNoTracking()
			.Select(x => new { x.ProjectName, x.TotalCredit })
			.ToListAsync(cancellationToken);

		return results.Select(x => (x.ProjectName, x.TotalCredit)).ToList();
	}
	private async Task<List<StatsTableEntity>> AggregateProjectStats(
		DateTime today,
		CancellationToken cancellationToken) {

		var yesterday = today.AddDays(-1);
		var weekStart = today.AddDays(-(int)today.DayOfWeek);
		var monthStart = new DateTime(today.Year, today.Month, 1);
		var yearStart = new DateTime(today.Year, 1, 1);

		// Get latest stats for each project using raw SQL
		var latestStats = await _dbContext.ProjectStats
			.FromSqlRaw(@"
				SELECT p.*
				FROM ""ProjectStats"" p
				INNER JOIN (
					SELECT ""ProjectName"", MAX(""YYYYMMDD"") as max_yyyymmdd
					FROM ""ProjectStats""
					GROUP BY ""ProjectName""
				) latest
				ON p.""ProjectName"" = latest.""ProjectName""
				AND p.""YYYYMMDD"" = latest.max_yyyymmdd")
			.AsNoTracking()
			.ToDictionaryAsync(c => c.ProjectName, v => new StatsTableEntity() {
				PartitionKey = StatsTableEntity.PROJECT_STATS,
				RowKey = v.ProjectName,
				CreditTotal = v.TotalCredit,
				LatestTaskDownloadTime = v.LatestTaskDownloadTime
			}, cancellationToken);

		var yesterdayStats = await GetEarliestProjectStatAfter(yesterday, cancellationToken);
		foreach (var stat in yesterdayStats) {
			latestStats.TryGetValue(stat.ProjectName, out var aggregated);
			if (aggregated != null) aggregated.CreditToday = aggregated.CreditTotal - stat.TotalCredit;
		}

		var weekStartStats = await GetEarliestProjectStatAfter(weekStart, cancellationToken);
		foreach (var stat in weekStartStats) {
			latestStats.TryGetValue(stat.ProjectName, out var aggregated);
			if (aggregated != null) aggregated.CreditThisWeek = aggregated.CreditTotal - stat.TotalCredit;
		}

		var monthStartStats = await GetEarliestProjectStatAfter(monthStart, cancellationToken);
		foreach (var stat in monthStartStats) {
			latestStats.TryGetValue(stat.ProjectName, out var aggregated);
			if (aggregated != null) aggregated.CreditThisMonth = aggregated.CreditTotal - stat.TotalCredit;
		}

		var yearStartStats = await GetEarliestProjectStatAfter(yearStart, cancellationToken);
		foreach (var stat in yearStartStats) {
			latestStats.TryGetValue(stat.ProjectName, out var aggregated);
			if (aggregated != null) aggregated.CreditThisYear = aggregated.CreditTotal - stat.TotalCredit;
		}

		return latestStats.Values.ToList();
	}

	private async Task<bool> UploadStatsToFunctionApp(HttpClient httpClient, StatsTableEntity stats, CancellationToken cancellationToken) {
		try {
			var url = $"{_functionAppOptions.BaseUrl.TrimEnd('/')}/api/stats";

			var request = new HttpRequestMessage(HttpMethod.Put, url) {
				Content = JsonContent.Create(stats)
			};

			if (!string.IsNullOrEmpty(_functionAppOptions.FunctionKey)) {
				request.Headers.Add("x-functions-key", _functionAppOptions.FunctionKey);
			}

			var response = await httpClient.SendAsync(request, cancellationToken);

			if (response.IsSuccessStatusCode) {
				_logger.LogDebug("Successfully uploaded stats for {PartitionKey}/{RowKey}", stats.PartitionKey, stats.RowKey);
				return true;
			} else {
				_logger.LogWarning("Failed to upload stats for {PartitionKey}/{RowKey}. Status: {StatusCode}",
					stats.PartitionKey, stats.RowKey, response.StatusCode);
				return false;
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error uploading stats for {PartitionKey}/{RowKey}", stats.PartitionKey, stats.RowKey);
			return false;
		}
	}
}
