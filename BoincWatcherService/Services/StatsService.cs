using BoincWatcherService.Models;
using BoincWatchService.Data;
using BoincWatchService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatchService.Services;

public class StatsService : IStatsService {
	private readonly ILogger<StatsService> _logger;
	private readonly StatsDbContext _dbContext;

	public StatsService(ILogger<StatsService> logger, StatsDbContext dbContext) {
		_logger = logger;
		_dbContext = dbContext;
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
}
