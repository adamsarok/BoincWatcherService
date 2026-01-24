using BoincWatcherService.Models;
using BoincWatcherService.Services.Interfaces;
using BoincWatchService.Data;
using BoincWatchService.Services.Interfaces;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatchService.Services;

public class StatsService(
	ILogger<StatsService> logger,
	StatsDbContext dbContext,
	IHttpClientFactory httpClientFactory,
	IFunctionAppService functionAppService) : IStatsService {

	public async Task<bool> UpsertHostStats(HostStats hostStats, CancellationToken cancellationToken = default) {
		try {
			if (string.IsNullOrEmpty(hostStats.YYYYMMDD) || string.IsNullOrEmpty(hostStats.HostName)) {
				logger.LogWarning("Invalid HostStats. YYYYMMDD and HostName are required.");
				return false;
			}

			var existingEntity = await dbContext.HostStats
				.FirstOrDefaultAsync(h => h.YYYYMMDD == hostStats.YYYYMMDD && h.HostName == hostStats.HostName, cancellationToken);

			if (existingEntity != null) {
				existingEntity.TotalCredit = hostStats.TotalCredit;
				existingEntity.Timestamp = hostStats.Timestamp ?? DateTimeOffset.UtcNow;
				existingEntity.LatestTaskDownloadTime = hostStats.LatestTaskDownloadTime;
				dbContext.HostStats.Update(existingEntity);
			} else {
				hostStats.Timestamp = hostStats.Timestamp ?? DateTimeOffset.UtcNow;
				dbContext.HostStats.Add(hostStats);
			}

			await dbContext.SaveChangesAsync(cancellationToken);
			logger.LogInformation("Successfully saved host stats for {HostName}", hostStats.HostName);
			return true;
		} catch (Exception ex) {
			logger.LogError(ex, "Error saving host stats for {HostName}", hostStats.HostName);
			return false;
		}
	}

	public async Task<bool> UpsertHostProjectStats(HostProjectStats hostProjectStats, CancellationToken cancellationToken = default) {
		try {
			if (string.IsNullOrEmpty(hostProjectStats.YYYYMMDD) || string.IsNullOrEmpty(hostProjectStats.HostName) || string.IsNullOrEmpty(hostProjectStats.ProjectName)) {
				logger.LogWarning("Invalid HostProjectStats. YYYYMMDD, HostName, and ProjectName are required.");
				return false;
			}

			var existingEntity = await dbContext.HostProjectStats
				.FirstOrDefaultAsync(h => h.YYYYMMDD == hostProjectStats.YYYYMMDD && h.HostName == hostProjectStats.HostName && h.ProjectName == hostProjectStats.ProjectName, cancellationToken);

			if (existingEntity != null) {
				existingEntity.TotalCredit = hostProjectStats.TotalCredit;
				existingEntity.Timestamp = hostProjectStats.Timestamp ?? DateTimeOffset.UtcNow;
				existingEntity.LatestTaskDownloadTime = hostProjectStats.LatestTaskDownloadTime;
				dbContext.HostProjectStats.Update(existingEntity);
			} else {
				hostProjectStats.Timestamp = hostProjectStats.Timestamp ?? DateTimeOffset.UtcNow;
				dbContext.HostProjectStats.Add(hostProjectStats);
			}

			await dbContext.SaveChangesAsync(cancellationToken);
			logger.LogInformation("Successfully saved host stats for {HostName} - {ProjectName}", hostProjectStats.HostName, hostProjectStats.ProjectName);
			return true;
		} catch (Exception ex) {
			logger.LogError(ex, "Error saving host stats for {HostName} - {ProjectName}", hostProjectStats.HostName, hostProjectStats.ProjectName);
			return false;
		}
	}

	public async Task<bool> UpsertProjectStats(ProjectStats projectStats, CancellationToken cancellationToken = default) {
		try {
			if (string.IsNullOrEmpty(projectStats.YYYYMMDD) || string.IsNullOrEmpty(projectStats.ProjectName)) {
				logger.LogWarning("Invalid ProjectStats. YYYYMMDD and ProjectName are required.");
				return false;
			}

			var existingEntity = await dbContext.ProjectStats
				.FirstOrDefaultAsync(p => p.YYYYMMDD == projectStats.YYYYMMDD && p.ProjectName == projectStats.ProjectName, cancellationToken);

			if (existingEntity != null) {
				existingEntity.TotalCredit = projectStats.TotalCredit;
				existingEntity.LatestTaskDownloadTime = projectStats.LatestTaskDownloadTime;
				dbContext.ProjectStats.Update(existingEntity);
			} else {
				dbContext.ProjectStats.Add(projectStats);
			}

			await dbContext.SaveChangesAsync(cancellationToken);
			logger.LogInformation("Successfully saved project stats for {ProjectName}", projectStats.ProjectName);
			return true;
		} catch (Exception ex) {
			logger.LogError(ex, "Error saving project stats for {ProjectName}", projectStats.ProjectName);
			return false;
		}
	}

	public async Task<bool> UpsertAggregateStats(CancellationToken cancellationToken = default) {
		try {
			if (!functionAppService.IsEnabled) {
				logger.LogWarning("Function app is not enabled. Skipping aggregate stats upload.");
				return false;
			}

			var today = DateTime.UtcNow.Date;

			// Aggregate HostStats
			var hostStatsAggregated = await AggregateHostStats(today, cancellationToken);

			// Aggregate ProjectStats
			var projectStatsAggregated = await AggregateProjectStats(today, cancellationToken);

			// Upload to function app
			var httpClient = httpClientFactory.CreateClient();
			var success = true;

			foreach (var hostStat in hostStatsAggregated) {
				if (!await functionAppService.UploadStatsToFunctionApp(httpClient, hostStat, cancellationToken)) {
					success = false;
				}
			}

			foreach (var projectStat in projectStatsAggregated) {
				if (!await functionAppService.UploadStatsToFunctionApp(httpClient, projectStat, cancellationToken)) {
					success = false;
				}
			}

			if (success) {
				logger.LogInformation("Successfully uploaded aggregate stats for {hostCount} hosts and {projectCount} projects",
					hostStatsAggregated.Count, projectStatsAggregated.Count);
			}

			return success;
		} catch (Exception ex) {
			logger.LogError(ex, "Error upserting aggregate stats");
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
		var latestStats = await dbContext.HostStats
			.FromSqlRaw(@"
				SELECT p.*
				FROM ""HostStats"" p
				INNER JOIN (
					SELECT ""HostName"", MAX(""YYYYMMDD"") as maxyyyymmdd
					FROM ""HostStats""
					GROUP BY ""HostName""
				) latest
				ON p.""HostName"" = latest.""HostName""
				AND p.""YYYYMMDD"" = latest.maxyyyymmdd")
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
		var results = await dbContext.HostStats
			.FromSqlRaw(@"
				SELECT p.*
				FROM ""HostStats"" p
				INNER JOIN (
					SELECT ""HostName"", MIN(""YYYYMMDD"") as minyyyymmdd
					FROM ""HostStats""
					WHERE ""YYYYMMDD"" >= {0}
					GROUP BY ""HostName""
				) earliest
				ON p.""HostName"" = earliest.""HostName""
				AND p.""YYYYMMDD"" = earliest.minyyyymmdd", fromDate.ToString("yyyyMMdd"))
			.AsNoTracking()
			.Select(x => new { x.HostName, x.TotalCredit })
			.ToListAsync(cancellationToken);

		return results.Select(x => (x.HostName, x.TotalCredit)).ToList();
	}
	private async Task<List<(string ProjectName, double TotalCredit)>> GetEarliestProjectStatAfter(DateTime fromDate,
		CancellationToken cancellationToken) {
		var results = await dbContext.ProjectStats
			.FromSqlRaw(@"
				SELECT p.*
				FROM ""ProjectStats"" p
				INNER JOIN (
					SELECT ""ProjectName"", MIN(""YYYYMMDD"") as minyyyymmdd
					FROM ""ProjectStats""
					WHERE ""YYYYMMDD"" >= {0}
					GROUP BY ""ProjectName""
				) earliest
				ON p.""ProjectName"" = earliest.""ProjectName""
				AND p.""YYYYMMDD"" = earliest.minyyyymmdd", fromDate.ToString("yyyyMMdd"))
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
		var latestStats = await dbContext.ProjectStats
			.FromSqlRaw(@"
				SELECT p.*
				FROM ""ProjectStats"" p
				INNER JOIN (
					SELECT ""ProjectName"", MAX(""YYYYMMDD"") as maxyyyymmdd
					FROM ""ProjectStats""
					GROUP BY ""ProjectName""
				) latest
				ON p.""ProjectName"" = latest.""ProjectName""
				AND p.""YYYYMMDD"" = latest.maxyyyymmdd")
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
}
