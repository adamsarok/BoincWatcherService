using BoincWatcherService.Models;
using BoincWatcherService.Services.Interfaces;
using BoincWatchService.Data;
using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService.Jobs;

public class FunctionAppUploadJob(ILogger<FunctionAppUploadJob> logger,
		IBoincService boincService,
		IStatsService statsService,
		StatsDbContext context,
		IProjectMappingService projectMappingService) : IJob {

	public async Task Execute(IJobExecutionContext context) {
		try {
			var partitionKey = DateTime.UtcNow.ToString("yyyyMMdd");
			logger.LogInformation("Stats upload job running at: {time}", DateTimeOffset.Now);

			var st = await boincService.GetHostStates();

			var aliveHosts = st.Where(x => x.State != HostStates.Down).ToList();

			foreach (var hostState in aliveHosts) {
				var hostStats = MapHostStateToDto(hostState, partitionKey);
				await statsService.UpsertHostStats(hostStats, context.CancellationToken);
			}

			if (aliveHosts.Any()) {
				var projectStats = await MapToProjectStatsTableEntitys(aliveHosts, partitionKey, context.CancellationToken);
				foreach (var projectStat in projectStats) {
					await statsService.UpsertProjectStats(projectStat, context.CancellationToken);
				}
				logger.LogInformation("Uploaded stats for {hostCount} hosts and {projectCount} projects",
					aliveHosts.Count, projectStats.Count());

				// Upload aggregate stats to function app
				await statsService.UpsertAggregateStats(context.CancellationToken);
			}
		} catch (Exception ex) {
			logger.LogError(ex, "Error occurred during stats upload job execution");
		}
	}

	private HostStats MapHostStateToDto(HostState hostState, string partitionKey) {
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
					LatestTaskDownloadTime = hostState.LatestTaskDownloadTime,
					TotalCredit = hostState.CoreClientState.Projects.Sum(x => x.HostTotalCredit),
				};
		}
	}


	private async Task<IEnumerable<ProjectStats>> MapToProjectStatsTableEntitys(IEnumerable<HostState> aliveHosts,
		string partitionKey,
		CancellationToken cancellationToken) {
		Dictionary<Guid, ProjectStats> projectStats = new();
		foreach (var host in aliveHosts) {
			foreach (var project in host.CoreClientState.Projects) {
				var tasks = host.CoreClientState.Results
					.Where(x => x.ProjectUrl == project.MasterUrl).ToList();
				DateTimeOffset? latestDownloadTime = tasks.Count == 0 ? null : tasks.Max(x => x.ReceivedTime);

				var dbProject = await projectMappingService.GetOrCreateProject(project.ProjectName, project.MasterUrl, cancellationToken);

				if (!projectStats.ContainsKey(dbProject.ProjectId)) {
					projectStats[dbProject.ProjectId] = new ProjectStats {
						YYYYMMDD = partitionKey,
						ProjectName = dbProject.ProjectNameDisplay, // ProjectName is not normalized in BoincRpc - same 
						TotalCredit = project.UserTotalCredit,
						LatestTaskDownloadTime = latestDownloadTime,
						MasterUrl = project.MasterUrl,
						ProjectId = dbProject.ProjectId,
					};
				} else {
					var projectStat = projectStats[dbProject.ProjectId];
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
