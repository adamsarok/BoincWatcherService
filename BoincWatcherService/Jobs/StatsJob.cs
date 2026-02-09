using BoincWatcherService.Models;
using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService.Jobs;

public class StatsJob : IJob {
	private readonly ILogger<StatsJob> _logger;
	private readonly IBoincService _boincService;
	private readonly IStatsService _statsService;

	public StatsJob(
		ILogger<StatsJob> logger,
		IBoincService boincService,
		IStatsService statsService) {
		_logger = logger;
		_boincService = boincService;
		_statsService = statsService;
	}

	public async Task Execute(IJobExecutionContext context) {
		try {
			string yyyymmdd = DateTime.UtcNow.ToString("yyyyMMdd");
			_logger.LogInformation("Stats upload job running at: {time}", DateTimeOffset.Now);

			var st = await _boincService.GetHostStates();

			var aliveHosts = st.Where(x => x.State != HostStates.Down).ToList();

			foreach (var hostState in aliveHosts) {
				var hostStats = MapHostStatsToDto(hostState, yyyymmdd);
				await _statsService.UpsertHostStats(hostStats, context.CancellationToken);
				var hostProjectStats = MapHostProjectStatsToDto(hostState, yyyymmdd);
				foreach (var hostProjectStat in hostProjectStats) {
					await _statsService.UpsertHostProjectStats(hostProjectStat, context.CancellationToken);
				}
			}

			if (aliveHosts.Any()) {
				var projectStats = MapToProjectStatsTableEntities(aliveHosts, yyyymmdd).ToList();
				foreach (var projectStat in projectStats) {
					await _statsService.UpsertProjectStats(projectStat, context.CancellationToken);
				}
				_logger.LogInformation("Uploaded stats for {hostCount} hosts and {projectCount} projects",
					aliveHosts.Count, projectStats.Count);

				// Upload aggregate stats to function app
				await _statsService.UpsertAggregateStats(context.CancellationToken);
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error occurred during StatsJob execution");
			// Don't rethrow - allow Quartz to continue scheduling
		}
	}
	private HostStats MapHostStatsToDto(HostState hostState, string partitionKey) {
		switch (hostState.State) {
			case HostStates.Down:
				return new HostStats {
					YYYYMMDD = partitionKey,
					HostName = hostState.HostName,
					LatestTaskDownloadTime = null,
					TotalCredit = 0,
				};
			default:
				return new HostStats {
					YYYYMMDD = partitionKey,
					HostName = hostState.HostName,
					LatestTaskDownloadTime = hostState.LatestTaskDownloadTimePerProjectUrl.Values.Any()
						? hostState.LatestTaskDownloadTimePerProjectUrl.Values.Max()
						: null,
					TotalCredit = hostState.CoreClientState.Projects.Sum(x => x.HostTotalCredit),
				};
		}
	}

	private IEnumerable<HostProjectStats> MapHostProjectStatsToDto(HostState hostState, string yyyymmdd) {
		foreach (var project in hostState.CoreClientState.Projects) {
			hostState.LatestTaskDownloadTimePerProjectUrl.TryGetValue(project.MasterUrl, out var latestDownloadTime);
			yield return new HostProjectStats {
				YYYYMMDD = yyyymmdd,
				HostName = hostState.HostName,
				ProjectName = project.ProjectName,
				LatestTaskDownloadTime = latestDownloadTime,
				TotalCredit = project.HostTotalCredit,
			};
		}
	}


	private IEnumerable<ProjectStats> MapToProjectStatsTableEntities(IEnumerable<HostState> aliveHosts, string yyyymmdd) {
		Dictionary<string, ProjectStats> projectStats = new();
		foreach (var host in aliveHosts) {
			foreach (var project in host.CoreClientState.Projects) {
				var tasks = host.CoreClientState.Results
					.Where(x => x.ProjectUrl == project.MasterUrl).ToList();
				DateTimeOffset? latestDownloadTime = tasks.Count == 0 ? null : tasks.Max(x => x.ReceivedTime);
				if (!projectStats.ContainsKey(project.ProjectName)) {
					projectStats[project.ProjectName] = new ProjectStats {
						YYYYMMDD = yyyymmdd,
						ProjectName = project.ProjectName,
						TotalCredit = project.UserTotalCredit,
						LatestTaskDownloadTime = latestDownloadTime
					};
				} else {
					var projectStat = projectStats[project.ProjectName];
					if (project.UserTotalCredit > projectStat.TotalCredit) {
						projectStat.TotalCredit = project.UserTotalCredit;
					}
					if (latestDownloadTime != null && (projectStat.LatestTaskDownloadTime == null || latestDownloadTime > projectStat.LatestTaskDownloadTime)) {
						projectStat.LatestTaskDownloadTime = latestDownloadTime;
					}
				}
			}
		}
		return projectStats.Values;
	}
}
