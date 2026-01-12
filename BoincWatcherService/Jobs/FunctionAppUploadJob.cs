using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
using Common.Models;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService.Jobs;

public class FunctionAppUploadJob : IJob {
	private readonly ILogger<FunctionAppUploadJob> _logger;
	private readonly IBoincService _boincService;
	private readonly IFunctionAppService _functionAppService;

	public FunctionAppUploadJob(
		ILogger<FunctionAppUploadJob> logger,
		IBoincService boincService,
		IFunctionAppService functionAppService) {
		_logger = logger;
		_boincService = boincService;
		_functionAppService = functionAppService;
	}

	public async Task Execute(IJobExecutionContext context) {
		try {
			var partitionKey = DateTime.UtcNow.ToString("yyyyMMdd");
			_logger.LogInformation("Function app upload job running at: {time}", DateTimeOffset.Now);

			var st = await _boincService.GetHostStates();

			var aliveHosts = st.Where(x => x.State != HostStates.Down).ToList();

			foreach (var hostState in aliveHosts) {
				var hostStats = MapHostStateToDto(hostState, partitionKey);
				await _functionAppService.PutHostStats(hostStats, context.CancellationToken);
			}

			if (aliveHosts.Any()) {
				var projectStats = MapToProjectStatsTableEntitys(aliveHosts, partitionKey);
				foreach (var projectStat in projectStats) {
					await _functionAppService.PutProjectStats(projectStat, context.CancellationToken);
				}
				_logger.LogInformation("Uploaded stats for {hostCount} hosts and {projectCount} projects",
					aliveHosts.Count, projectStats.Count());
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error occurred during FunctionAppUploadJob execution");
		}
	}

	private HostStatsTableEntity MapHostStateToDto(HostState hostState, string partitionKey) {
		switch (hostState.State) {
			case HostStates.Down:
				return new HostStatsTableEntity {
					PartitionKey = partitionKey,
					RowKey = hostState.HostName,
					LatestTaskDownloadTime = null,
					TotalCredit = 0,
					RAC = 0
				};
			default:
				return new HostStatsTableEntity {
					PartitionKey = partitionKey,
					RowKey = hostState.HostName,
					LatestTaskDownloadTime = hostState.LatestTaskDownloadTime,
					TotalCredit = hostState.CoreClientState.Projects.Sum(x => x.HostTotalCredit),
					RAC = hostState.CoreClientState.Projects.Sum(x => x.HostAverageCredit)
				};
		}
	}

	private IEnumerable<ProjectStatsTableEntity> MapToProjectStatsTableEntitys(IEnumerable<HostState> aliveHosts, string partitionKey) {
		Dictionary<string, ProjectStatsTableEntity> projectStats = new();
		foreach (var host in aliveHosts) {
			foreach (var project in host.CoreClientState.Projects) {
				var tasks = host.CoreClientState.Results
					.Where(x => x.ProjectUrl == project.MasterUrl).ToList();
				DateTimeOffset? latestDownloadTime = tasks.Count == 0 ? null : tasks.Max(x => x.ReceivedTime);
				if (!projectStats.ContainsKey(project.ProjectName)) {
					projectStats[project.ProjectName] = new ProjectStatsTableEntity {
						PartitionKey = partitionKey,
						RowKey = project.ProjectName,
						TotalCredit = project.UserTotalCredit,
						RAC = project.UserAverageCredit,
						LatestTaskDownloadTime = latestDownloadTime
					};
				} else {
					var projectStat = projectStats[project.ProjectName];
					if (project.UserTotalCredit > projectStat.TotalCredit) {
						projectStat.TotalCredit = project.UserTotalCredit;
					}
					if (project.UserAverageCredit > projectStat.RAC) {
						projectStat.RAC = project.UserAverageCredit;
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
