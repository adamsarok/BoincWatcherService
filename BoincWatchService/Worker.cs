using BoincWatchService.Options;
using BoincWatchService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatchService {
	public class Worker : BackgroundService {
		private readonly ILogger<Worker> _logger;
		private readonly SchedulingOptions _schedulingSettings;
		private readonly IBoincService _boincService;
		private readonly IMailService _mailService;

		public Worker(
			ILogger<Worker> logger,
			IOptions<SchedulingOptions> schedulingSettings,
			IBoincService boincService,
			IMailService mailService) {
			_logger = logger;
			_schedulingSettings = schedulingSettings.Value;
			_boincService = boincService;
			_mailService = mailService;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
			var clientStatesToSend = _schedulingSettings.SendNotificationOnStates;
			while (!stoppingToken.IsCancellationRequested) {
				_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				var st = await _boincService.GetHostStates();
				var clientsToSend = st.Where(x => clientStatesToSend.Contains(x.State)).ToList();
				if (clientsToSend.Count > 0) {
					var msg = JsonSerializer.Serialize(clientsToSend, new JsonSerializerOptions { WriteIndented = true });
					_mailService.SendMail($"Boinc client status {DateTime.Now}", msg);
				}
				await Task.Delay(_schedulingSettings.ScheduleIntervalMinutes * 60 * 1000, stoppingToken);
			}
		}
	}
}
