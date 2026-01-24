using BoincRpc;
using BoincWatchService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService.Services {
	public class BoincService : IBoincService {
		private readonly List<BoincHostOptions> hosts;
		private readonly ILogger<BoincService> logger;

		public BoincService(IOptions<List<BoincHostOptions>> hosts, ILogger<BoincService> logger) {
			this.hosts = hosts.Value;
			this.logger = logger;
		}


		public async Task<IEnumerable<HostState>> GetHostStates() {
			List<HostState> results = new List<HostState>();
			foreach (var host in hosts) {
				var result = new HostState() {
					IP = host.IP
				};
				RpcClient client = null;
				try {
					client = new RpcClient();
					await client.ConnectAsync(host.IP, host.Port);
					await UpdateHostData(host, client, result);
				} catch (Exception ex) {
					logger.LogError(ex, "Error connecting to host {HostIP}", host.IP);
					result.State = HostStates.Down;
					result.ErrorMsg = ex.Message;
				} finally {
					if (client != null) {
						try {
							client.Close();
						} catch {
							// Ignore errors during cleanup
						}
					}
				}
				results.Add(result);
			}
			return results;
		}

		private async Task UpdateHostData(BoincHostOptions host, RpcClient client, HostState result) {
			await client.AuthorizeAsync(host.Password);
			var stats = await client.GetStateAsync();
			result.CoreClientState = stats;
			var runningTasks = stats.Results.Where(x => x.CurrentCpuTime.TotalSeconds > 1);
			result.HostName = stats.HostInfo.DomainName;
			result.TasksStarted = runningTasks.Count();
			if (stats.Results.Any()) {
				result.LatestTaskDownloadTimePerProjectUrl = stats.Results
					.GroupBy(key => key.ProjectUrl)
					.Select(g => new { ProjectUrl = g.Key, MaxReceivedTime = g.Max(o => o.ReceivedTime) })
					.ToDictionary(key => key.ProjectUrl, value => value.MaxReceivedTime);
				result.State = HostStates.NoRunningTasks;
				if (result.TasksStarted > 0) result.State = HostStates.OK;
			} else result.State = HostStates.NoTasks;
		}
	}

	public class HostState {
		public string HostName { get; set; }
		public string IP { get; set; }
		public Dictionary<string, DateTimeOffset> LatestTaskDownloadTimePerProjectUrl { get; set; } = new();
		public int TasksStarted { get; set; }
		public HostStates State { get; set; }
		public string ErrorMsg { get; set; }
		public enum HostStates { Down, OK, NoRunningTasks, NoTasks }
		public CoreClientState CoreClientState { get; set; }
	}
}
