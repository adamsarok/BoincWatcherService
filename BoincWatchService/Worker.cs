using BoincWatchService.DTO;
using BoincWatchService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
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
					await _functionAppService.PutHostStats(hostStats);
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

		private HostStatsDto MapHostStateToDto(HostState hostState) {
			switch (hostState.State) {
				case HostStates.Down:
					return new HostStatsDto {
						PartitionKey = DateTime.UtcNow.ToString("yyyyMMdd"),
						RowKey = hostState.HostName,
						LastTaskCompletedTimestamp = null,
						TotalCredit = 0, // not good, we should really show the last known value
						RAC = 0
					};
				default:
					return new HostStatsDto {
						PartitionKey = DateTime.UtcNow.ToString("yyyyMMdd"),
						RowKey = hostState.HostName,
						LastTaskCompletedTimestamp = hostState.LatestTaskDownloadTime,
						TotalCredit = hostState.CoreClientState.Projects.Sum(x => x.HostTotalCredit),
						RAC = hostState.CoreClientState.Projects.Sum(x => x.HostAverageCredit)
					};
			}
		}
	}
}
