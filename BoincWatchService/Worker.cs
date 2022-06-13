using BoincRpc;
using BoincWatchService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService {
	public class Worker : BackgroundService {
		private readonly ILogger<Worker> _logger;
		private readonly IConfigService _conf;
		private readonly IBoincService _boincService;
		private readonly IMailService _mailService;
		public Worker(ILogger<Worker> logger, IConfigService conf, IBoincService boincService, IMailService mailService) {
			_logger = logger;
			_conf = conf;
			_boincService = boincService;
			_mailService = mailService;
		}
		protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
			var scheduleSettings = _conf.GetSchedulingSettings();
			while (!stoppingToken.IsCancellationRequested) {
				_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				var st = await _boincService.GetHostStates();
				var clientsToSend = st.Where(x => scheduleSettings.SendClientStates.Contains(x.State)).ToList();
				if (clientsToSend.Count > 0) {
					StringBuilder sb = new StringBuilder();
					clientsToSend.ForEach((x) => {
						sb.AppendLine(x.ToString());
						sb.AppendLine();
					});
					_mailService.SendMail($"Boinc client status {DateTime.Now}", sb.ToString());
				}
				await Task.Delay(scheduleSettings.ScheduleIntervalMinutes * 60 * 1000, stoppingToken);
			}
		}
		
	}
}
