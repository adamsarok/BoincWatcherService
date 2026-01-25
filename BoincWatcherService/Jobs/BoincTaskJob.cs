using BoincWatcherService.Models;
using BoincWatchService.Data;
using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService.Jobs;

public class BoincTaskJob(ILogger<BoincTaskJob> logger,
		IBoincService boincService,
		StatsDbContext context) : IJob {

	public async Task Execute(IJobExecutionContext context) {
		logger.LogInformation("Boinc task job running at: {time}", DateTimeOffset.Now);

		var st = await boincService.GetHostStates();

		var aliveHosts = st.Where(x => x.State != HostStates.Down).ToList();

		var allApps = new List<BoincApp>();
		var allTasks = new List<BoincTask>();
		foreach (var hostState in aliveHosts) {
			var apps = GetApps(hostState).ToList();
			allApps.AddRange(apps);

			var tasks = GetRunningTasks(hostState).ToList();
			allTasks.AddRange(tasks);
		}

		if (allApps.Count > 0) {
			await UpsertBoincApps(allApps, context.CancellationToken);
			logger.LogInformation("Successfully upserted {Count} apps", allApps.Count);
		}

		if (allTasks.Count > 0) {
			await UpsertBoincTasks(allTasks, context.CancellationToken);
			logger.LogInformation("Successfully upserted {Count} tasks", allTasks.Count);
		}
	}

	private IEnumerable<BoincApp> GetApps(HostState hostState) {
		foreach (var app in hostState.CoreClientState.Apps) {
			yield return new BoincApp {
				ProjectName = hostState.CoreClientState.Projects
					.FirstOrDefault(p => p.MasterUrl == app.ProjectUrl)?.ProjectName ?? string.Empty,
				ProjectUrl = app.ProjectUrl,
				Name = app.Name,
				UpdatedAt = DateTime.UtcNow,
				UserFriendlyName = app.UserFriendlyName
			};
		}
	}

	private IEnumerable<BoincTask> GetRunningTasks(HostState hostState) {
		var projectDict = hostState.CoreClientState.Projects.ToDictionary(x => x.MasterUrl);
		var wus = hostState.CoreClientState.Workunits.ToDictionary(x => (x.ProjectUrl, x.Name));

		foreach (var result in hostState.CoreClientState.Results) {
			wus.TryGetValue((result.ProjectUrl, result.WorkunitName), out var wu);
			projectDict.TryGetValue(result.ProjectUrl, out var project);
			yield return new BoincTask {
				HostName = hostState.HostName,
				ProjectName = project == null ? "" : project.ProjectName,
				TaskName = result.Name,
				AppName = wu == null ? "" : wu.AppName,
				CurrentCpuTime = result.CurrentCpuTime,
				EstimatedCpuTimeRemaining = result.EstimatedCpuTimeRemaining,
				ElapsedTime = result.ElapsedTime,
				FractionDone = result.FractionDone,
				ReceivedTime = result.ReceivedTime,
				UpdatedAt = DateTime.UtcNow
			};
		}
	}

	public async Task<bool> UpsertBoincTasks(List<BoincTask> tasks, CancellationToken cancellationToken = default) {
		try {
			if (tasks == null || tasks.Count == 0) {
				logger.LogWarning("No tasks to upsert.");
				return false;
			}
			await context.BulkInsertOrUpdateAsync(tasks, cancellationToken: cancellationToken);
			logger.LogInformation("Successfully upserted {Total} tasks.", tasks.Count);
			return true;
		} catch (Exception ex) {
			logger.LogError(ex, "Error upserting {Count} tasks", tasks.Count);
			return false;
		}
	}

	public async Task<bool> UpsertBoincApps(List<BoincApp> apps, CancellationToken cancellationToken = default) {
		try {
			if (apps == null || apps.Count == 0) {
				logger.LogWarning("No apps to upsert.");
				return false;
			}
			var distinctApps = apps
				.GroupBy(a => new { a.ProjectName, a.Name })
				.Select(g => g.OrderByDescending(a => a.UserFriendlyName).First())
				.ToList();
			await context.BulkInsertOrUpdateAsync(distinctApps, cancellationToken: cancellationToken);
			logger.LogInformation("Successfully upserted {Total} apps.", distinctApps.Count);
			return true;
		} catch (Exception ex) {
			logger.LogError(ex, "Error upserting {Count} apps", apps.Count);
			return false;
		}
	}
}
