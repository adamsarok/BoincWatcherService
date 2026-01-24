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

		var allTasks = new List<BoincTask>();
		foreach (var hostState in aliveHosts) {
			var tasks = GetRunningTasks(hostState).ToList();
			allTasks.AddRange(tasks);
		}

		if (allTasks.Count > 0) {
			await UpsertBoincTasks(allTasks, context.CancellationToken);
			logger.LogInformation("Successfully upserted {Count} tasks", allTasks.Count);
		}
	}

	private IEnumerable<BoincTask> GetRunningTasks(HostState hostState) {
		var projectDict = hostState.CoreClientState.Projects.ToDictionary(x => x.MasterUrl);
		foreach (var result in hostState.CoreClientState.Results) {
			yield return new BoincTask {
				HostName = hostState.HostName,
				ProjectName = projectDict[result.ProjectUrl].ProjectName,
				TaskName = result.Name,
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
}
