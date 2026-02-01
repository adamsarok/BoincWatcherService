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
	StatsDbContext context,
	IHttpClientFactory httpClientFactory,
	IFunctionAppService functionAppService) : IStatsService {

	public async Task<bool> UpsertHostStats(HostStats hostStats, CancellationToken cancellationToken = default) {
		try {
			if (string.IsNullOrEmpty(hostStats.YYYYMMDD) || string.IsNullOrEmpty(hostStats.HostName)) {
				logger.LogWarning("Invalid HostStats. YYYYMMDD and HostName are required.");
				return false;
			}

			var existingEntity = await context.HostStats
				.FirstOrDefaultAsync(h => h.YYYYMMDD == hostStats.YYYYMMDD && h.HostName == hostStats.HostName, cancellationToken);

			if (existingEntity != null) {
				existingEntity.TotalCredit = hostStats.TotalCredit;
				existingEntity.Timestamp = hostStats.Timestamp ?? DateTimeOffset.UtcNow;
				existingEntity.LatestTaskDownloadTime = hostStats.LatestTaskDownloadTime;
				context.HostStats.Update(existingEntity);
			} else {
				hostStats.Timestamp = hostStats.Timestamp ?? DateTimeOffset.UtcNow;
				context.HostStats.Add(hostStats);
			}

			await context.SaveChangesAsync(cancellationToken);
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

			var existingEntity = await context.HostProjectStats
				.FirstOrDefaultAsync(h => h.YYYYMMDD == hostProjectStats.YYYYMMDD && h.HostName == hostProjectStats.HostName && h.ProjectName == hostProjectStats.ProjectName, cancellationToken);

			if (existingEntity != null) {
				existingEntity.TotalCredit = hostProjectStats.TotalCredit;
				existingEntity.Timestamp = hostProjectStats.Timestamp ?? DateTimeOffset.UtcNow;
				existingEntity.LatestTaskDownloadTime = hostProjectStats.LatestTaskDownloadTime;
				context.HostProjectStats.Update(existingEntity);
			} else {
				hostProjectStats.Timestamp = hostProjectStats.Timestamp ?? DateTimeOffset.UtcNow;
				context.HostProjectStats.Add(hostProjectStats);
			}

			await context.SaveChangesAsync(cancellationToken);
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

			var existingEntity = await context.ProjectStats
				.FirstOrDefaultAsync(p => p.YYYYMMDD == projectStats.YYYYMMDD && p.ProjectName == projectStats.ProjectName, cancellationToken);

			if (existingEntity != null) {
				existingEntity.TotalCredit = projectStats.TotalCredit;
				existingEntity.LatestTaskDownloadTime = projectStats.LatestTaskDownloadTime;
				context.ProjectStats.Update(existingEntity);
			} else {
				context.ProjectStats.Add(projectStats);
			}

			await context.SaveChangesAsync(cancellationToken);
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

			// Upload to function app
			var httpClient = httpClientFactory.CreateClient();
			var success = true;

			var hostStatsAggregated = await AggregateHostStats(today, cancellationToken);
			foreach (var hostStat in hostStatsAggregated) {
				if (!await functionAppService.UploadStatsToFunctionApp(httpClient, hostStat, cancellationToken)) {
					success = false;
				}
			}

			var projectStatsAggregated = await AggregateProjectStats(today, cancellationToken);
			foreach (var projectStat in projectStatsAggregated) {
				if (!await functionAppService.UploadStatsToFunctionApp(httpClient, projectStat, cancellationToken)) {
					success = false;
				}
			}

			var resultStatsAggregated = await AggregateResultStats(today, cancellationToken);
			foreach (var resultStat in resultStatsAggregated) {
				if (!await functionAppService.UploadAppRuntimeToFunctionApp(httpClient, resultStat, cancellationToken)) {
					success = false;
				}
			}

			var efficiencyStatsAggregated = await AggregateEfficiencyStats(today, cancellationToken);
			foreach (var efficiencyStat in efficiencyStatsAggregated) {
				if (!await functionAppService.UploadEfficiencyToFunctionApp(httpClient, efficiencyStat, cancellationToken)) {
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

	private async Task<List<AppRuntimeTableEntity>> AggregateResultStats(DateTime today,
		CancellationToken cancellationToken) {
		var weekStart = today.AddDays(-(int)today.DayOfWeek);
		var monthStart = new DateTime(today.Year, today.Month, 1);
		var yearStart = new DateTime(today.Year, 1, 1);

		var todayResults = await GetResultsAggregateAfter(today, cancellationToken);
		var weekResults = await GetResultsAggregateAfter(weekStart, cancellationToken);
		var monthResults = await GetResultsAggregateAfter(monthStart, cancellationToken);
		var yearResults = await GetResultsAggregateAfter(yearStart, cancellationToken);
		var allTimeStats = await GetResultsAggregateAfter(DateTime.MinValue, cancellationToken);
		var resultDict = new Dictionary<(string HostName, string ProjectName, string AppName), AppRuntimeTableEntity>();
		foreach (var row in allTimeStats) {
			resultDict.Add((row.HostName, row.ProjectName, row.AppName), new AppRuntimeTableEntity(row.HostName, row.ProjectName, row.AppName) {
				CPUHoursTotal = row.CPUHours
			});
		}
		foreach (var row in yearResults) {
			resultDict[(row.HostName, row.ProjectName, row.AppName)].CPUHoursThisYear = row.CPUHours;
		}
		foreach (var row in monthResults) {
			resultDict[(row.HostName, row.ProjectName, row.AppName)].CPUHoursThisMonth = row.CPUHours;
		}
		foreach (var row in weekResults) {
			resultDict[(row.HostName, row.ProjectName, row.AppName)].CPUHoursThisWeek = row.CPUHours;
		}
		foreach (var row in todayResults) {
			resultDict[(row.HostName, row.ProjectName, row.AppName)].CPUHoursToday = row.CPUHours;
		}
		return resultDict.Values.ToList();
	}

	private record ResultsAggregate(string HostName, string ProjectName, string AppName, double CPUHours);
	private async Task<IEnumerable<ResultsAggregate>> GetResultsAggregateAfter(DateTime fromDate,
		CancellationToken cancellationToken) {
		var fromDateUtc = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
		return await context.BoincTasks
			.Where(r => r.UpdatedAt >= fromDateUtc)
			.GroupBy(r => new { r.HostName, r.ProjectName, r.AppName })
			.Select(x => new ResultsAggregate(
				x.Key.HostName,
				x.Key.ProjectName,
				x.Key.AppName,
				x.Sum(r => r.CurrentCpuTime.TotalHours))
			)
			.AsNoTracking()
			.ToListAsync(cancellationToken);
	}

	private async Task<List<StatsTableEntity>> AggregateHostStats(
		DateTime today,
		CancellationToken cancellationToken) {

		var yesterday = today.AddDays(-1);
		var weekStart = today.AddDays(-(int)today.DayOfWeek);
		var monthStart = new DateTime(today.Year, today.Month, 1);
		var yearStart = new DateTime(today.Year, 1, 1);

		// Get latest stats for each project using raw SQL
		var latestStats = await context.HostStats
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
		var results = await context.HostStats
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
		var results = await context.ProjectStats
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
		var latestStats = await context.ProjectStats
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

	private async Task<List<EfficiencyTableEntity>> AggregateEfficiencyStats(
		DateTime today,
		CancellationToken cancellationToken) {

		Dictionary<(string HostName, string ProjectName), EfficiencyTableEntity> results = new();
		var workHours = await context.BoincTasks
			.Where(w => w.ProjectName != "WUProp@Home")
			.GroupBy(t => new { t.HostName, t.ProjectName })
			.Select(g => new {
				g.Key.HostName,
				g.Key.ProjectName,
				DateFrom = g.Min(m => m.UpdatedAt),
				DateTo = g.Max(m => m.UpdatedAt),
				TotalCpuTimeHours = g.Sum(x => x.CurrentCpuTime.TotalHours)
			})
			.ToListAsync(cancellationToken);

		foreach (var group in workHours) {
			results.Add((group.HostName, group.ProjectName), new EfficiencyTableEntity(group.HostName, group.ProjectName) {
				CPUHoursTotal = group.TotalCpuTimeHours
			});
		}

		foreach (var group in workHours) {
			if (group.DateFrom.Date == group.DateTo.Date) continue;
			var startPoints = await context.HostProjectStats.FindAsync(
				new object[] { group.DateFrom.ToString("yyyyMMdd"), group.HostName, group.ProjectName },
				cancellationToken);
			if (startPoints == null) continue;
			var endPoints = await context.HostProjectStats.FindAsync(
				new object[] { group.DateTo.ToString("yyyyMMdd"), group.HostName, group.ProjectName },
				cancellationToken);
			if (endPoints == null) continue;
			var result = results[(group.HostName, group.ProjectName)];
			result.CPUHoursTotal = group.TotalCpuTimeHours;
			result.PointsTotal = endPoints.TotalCredit - startPoints.TotalCredit;
			result.PointsPerCPUHour = group.TotalCpuTimeHours != 0 ? (endPoints.TotalCredit - startPoints.TotalCredit) / group.TotalCpuTimeHours : 0;
		}

		return results.Values.ToList();
	}
}
