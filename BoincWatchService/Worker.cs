using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
using Common.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService {
	public class Worker : BackgroundService {
		private readonly ILogger<Worker> _logger;
		private readonly SchedulingOptions _schedulingSettings;
		private readonly IBoincService _boincService;
		private readonly IMailService _mailService;
		private readonly IFunctionAppService _functionAppService;

		public Worker(
			ILogger<Worker> logger,
			IOptions<SchedulingOptions> schedulingSettings,
			IBoincService boincService,
			IMailService mailService,
			IFunctionAppService functionAppService) {
			_logger = logger;
			_schedulingSettings = schedulingSettings.Value;
			_boincService = boincService;
			_mailService = mailService;
			_functionAppService = functionAppService;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
			var clientStatesToSend = _schedulingSettings.SendNotificationOnStates;
			while (!stoppingToken.IsCancellationRequested) {
				_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				var st = await _boincService.GetHostStates();

				// Upload each host state to Function App
				foreach (var hostState in st) {
					var hostStats = MapHostStateToDto(hostState);
					await _functionAppService.PutHostStats(hostStats, stoppingToken);
				}

				var aliveHosts = st.Where(x => x.State != HostStates.Down).ToList();
				if (aliveHosts.Any()) {
					var projectStats = MapToProjectStatsTableEntitys(aliveHosts);
					foreach (var projectStat in projectStats) {
						await _functionAppService.PutProjectStats(projectStat, stoppingToken);
					}
				}

				if (clientStatesToSend != null && clientStatesToSend.Count > 0) {
					var clientsToSend = st.Where(x => clientStatesToSend.Contains(x.State)).ToList();
					if (clientsToSend.Count > 0) {
						var msg = JsonSerializer.Serialize(clientsToSend, new JsonSerializerOptions { WriteIndented = true });
						await _mailService.SendMail($"Boinc client status {DateTime.Now}", msg);
					}
				}

				await Task.Delay(TimeSpan.FromMinutes(_schedulingSettings.ScheduleIntervalMinutes), stoppingToken);
			}
		}

		private HostStatsTableEntity MapHostStateToDto(HostState hostState) {
			switch (hostState.State) {
				case HostStates.Down:
					return new HostStatsTableEntity {
						PartitionKey = DateTime.UtcNow.ToString("yyyyMMdd"),
						RowKey = hostState.HostName,
						LatestTaskDownloadTime = null,
						TotalCredit = 0, // not good, we should really show the last known value
						RAC = 0
					};
				default:
					return new HostStatsTableEntity {
						PartitionKey = DateTime.UtcNow.ToString("yyyyMMdd"),
						RowKey = hostState.HostName,
						LatestTaskDownloadTime = hostState.LatestTaskDownloadTime,
						TotalCredit = hostState.CoreClientState.Projects.Sum(x => x.HostTotalCredit),
						RAC = hostState.CoreClientState.Projects.Sum(x => x.HostAverageCredit)
					};
			}
		}

		private IEnumerable<ProjectStatsTableEntity> MapToProjectStatsTableEntitys(IEnumerable<HostState> aliveHosts) {
			Dictionary<string, ProjectStatsTableEntity> projectStats = new();
			foreach (var host in aliveHosts) {
				foreach (var project in host.CoreClientState.Projects) {
					var tasks = host.CoreClientState.Results
						.Where(x => x.ProjectUrl == project.MasterUrl).ToList();
					DateTimeOffset? latestDownloadTime = tasks.Count == 0 ? null : tasks.Max(x => x.ReceivedTime);
					if (!projectStats.ContainsKey(project.ProjectName)) {
						projectStats[project.ProjectName] = new ProjectStatsTableEntity {
							PartitionKey = DateTime.UtcNow.ToString("yyyyMMdd"),
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
}
