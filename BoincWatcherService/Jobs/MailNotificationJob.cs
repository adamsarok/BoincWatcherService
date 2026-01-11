using BoincWatchService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BoincWatchService.Jobs;

public class MailNotificationJob : IJob {
	private readonly ILogger<MailNotificationJob> _logger;
	private readonly IBoincService _boincService;
	private readonly IMailService _mailService;
	private readonly MailOptions _mailOptions;

	public MailNotificationJob(
		ILogger<MailNotificationJob> logger,
		IBoincService boincService,
		IMailService mailService,
		IOptions<Options.MailOptions> mailOptions) {
		_logger = logger;
		_boincService = boincService;
		_mailService = mailService;
		_mailOptions = mailOptions.Value;
	}

	public async Task Execute(IJobExecutionContext context) {
		try {
			_logger.LogInformation("Mail notification job running at: {time}", DateTimeOffset.Now);

			var st = await _boincService.GetHostStates();
			var clientStatesToSend = _mailOptions.SendNotificationOnStates;

			if (clientStatesToSend != null && clientStatesToSend.Count > 0) {
				var clientsToSend = st.Where(x => clientStatesToSend.Contains(x.State)).ToList();
				if (clientsToSend.Count > 0) {
					var msg = JsonSerializer.Serialize(clientsToSend, new JsonSerializerOptions { WriteIndented = true });
					await _mailService.SendMail($"Boinc client status {DateTime.Now}", msg);
					_logger.LogInformation("Mail notification sent for {count} hosts", clientsToSend.Count);
				}
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error occurred during MailNotificationJob execution");
		}
	}
}
