using BoincRpc;
using BoincWatchService.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService.Services {
	public class BoincService : IBoincService {
		private readonly List<BoincHostOptions> _hosts;

		public BoincService(IOptions<List<BoincHostOptions>> hosts) {
			_hosts = hosts.Value;
		}


		public async Task<IEnumerable<HostState>> GetHostStates() {
			List<HostState> results = new List<HostState>();
			foreach (var host in _hosts) {
				var result = new HostState() {
					IP = host.IP
				};
				RpcClient client = null;
				try {
					client = new RpcClient();
					await client.ConnectAsync(host.IP, host.Port);
					await UpdateHostData(host, client, result);
				} catch (OperationCanceledException) {
					result.State = HostStates.Down;
					result.ErrorMsg = "Connection timeout";
				} catch (Exception ex) {
					result.State = HostStates.Down;
					result.ErrorMsg = ex.Message;
				} finally {
					// Properly dispose the RPC client to release the connection
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
			var runningTasks = stats.Results.Where(x => x.CurrentCpuTime.TotalSeconds > 1);
			result.HostName = stats.HostInfo.DomainName;
			result.TasksStarted = runningTasks.Count();
			if (stats.Results.Any()) {
				result.LatestTaskDownloadTime = stats.Results.Max(x => x.ReceivedTime);
				result.State = HostStates.NoRunningTasks;
				if (result.TasksStarted > 0) result.State = HostStates.OK;
			} else result.State = HostStates.NoTasks;
		}
	}

	public interface IBoincService {
		public Task<IEnumerable<HostState>> GetHostStates();
	}

	public class HostState {
		public string HostName { get; set; }
		public string IP { get; set; }
		public DateTimeOffset LatestTaskDownloadTime { get; set; }
		public int TasksStarted { get; set; }
		public HostStates State { get; set; }
		public string ErrorMsg { get; set; }
		public enum HostStates { Down, OK, NoRunningTasks, NoTasks }
	}
}
