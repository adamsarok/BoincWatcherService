using BoincWatchService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Quartz;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BoincWatchService.Jobs;

public class MailNotificationJob(ILogger<MailNotificationJob> logger,
		IBoincService boincService,
		IMailService mailService,
		IOptions<Options.MailOptions> mailOptions,
		IVariantFeatureManager featureManager) : IJob {

	public async Task Execute(IJobExecutionContext context) {
		try {
			if (!await featureManager.IsEnabledAsync("TaskJob")) {
				return;
			}

			logger.LogInformation("Mail notification job running at: {time}", DateTimeOffset.Now);

			var st = await boincService.GetHostStates();
			var clientStatesToSend = mailOptions.Value?.SendNotificationOnStates;

			if (clientStatesToSend != null && clientStatesToSend.Count > 0) {
				var clientsToSend = st.Where(x => clientStatesToSend.Contains(x.State)).ToList();
				if (clientsToSend.Count > 0) {
					var msg = JsonSerializer.Serialize(clientsToSend, new JsonSerializerOptions { WriteIndented = true });
					await mailService.SendMail($"Boinc client status {DateTime.Now}", msg);
					logger.LogInformation("Mail notification sent for {count} hosts", clientsToSend.Count);
				}
			}
		} catch (Exception ex) {
			logger.LogError(ex, "Error occurred during MailNotificationJob execution");
		}
	}
}
