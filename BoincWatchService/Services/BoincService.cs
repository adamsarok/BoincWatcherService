using BoincRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService.Services {
	public class BoincService : IBoincService {
		private readonly IConfigService _configService;
		public BoincService(IConfigService configService) {
			_configService = configService;
		}
		public async Task<IEnumerable<HostState>> GetHostStates() {
			List<HostState> results = new List<HostState>();
			foreach (var host in _configService.GetHosts()) {
				var result = new HostState() {
					IP = host.IP
				};
				var client = new RpcClient();
				try {
					await client.ConnectAsync(host.IP, host.Port);
					await GetHostData(host, client, result);
				} catch (Exception ex) {
					result.State = HostStates.Down;
					result.ErrorMsg = ex.Message;
				}
				results.Add(result);
			}
			return results;
		}
		private async Task GetHostData(HostSettings host, RpcClient client, HostState result) {
			await client.AuthorizeAsync(host.Password);
			Result[] tasks = await client.GetResultsAsync();
			var boincHost = await client.GetHostInfoAsync();
			var runningTasks = tasks.Where(x => x.CurrentCpuTime.TotalSeconds > 1);
			result.HostName = boincHost.DomainName;
			result.TasksStarted = runningTasks.Count();
			if (tasks.Count() > 0) {
				result.LatestTaskDownloadTime = tasks.Max(x => x.ReceivedTime);
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
		public override string ToString() {
			return @$"[HostName: {HostName}
IP: {IP}
Client state: {State}
Task last downloaded at: {LatestTaskDownloadTime}
Number of tasks started: {TasksStarted}
Error Msg: {ErrorMsg}]";
		}
	}
}
