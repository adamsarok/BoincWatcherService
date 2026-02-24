using BoincWatchService.Services.Interfaces;
using Microsoft.FeatureManagement;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BoincWatchService.Services {
	public class MailService(IOptions<MailOptions> mailSettings,
			IVariantFeatureManager featureManager
		) : IMailService {

		public async Task SendMail(string subject, string body) {
			if (!await featureManager.IsEnabledAsync("MailJob")) {
				return;
			}
			var settings = mailSettings?.Value ?? throw new InvalidOperationException("Mail settings are not configured.");
			using (var smtp = new SmtpClient(settings.SmtpHost, settings.SmtpPort)) {
				smtp.UseDefaultCredentials = false;
				smtp.Credentials = new NetworkCredential(settings.UserName, settings.Password);
				using (var mailMessage = new MailMessage()) {
					mailMessage.From = new MailAddress(settings.SenderAddress);
					mailMessage.To.Add(settings.ToAddress);
					mailMessage.Body = body;
					mailMessage.Subject = subject;
					await smtp.SendMailAsync(mailMessage);
				}
			}
		}
	}
}
