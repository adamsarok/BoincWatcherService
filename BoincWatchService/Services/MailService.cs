using BoincWatchService.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace BoincWatchService.Services {
	public class MailService : IMailService {
		private readonly MailOptions _mailSettings;

		public MailService(IOptions<MailOptions> mailSettings) {
			_mailSettings = mailSettings.Value;
		}

		public async void SendMail(string subject, string body) {
			if (!_mailSettings.IsEnabled) return;
			using (var smtp = new SmtpClient(_mailSettings.SmtpHost, _mailSettings.SmtpPort)) {
				smtp.UseDefaultCredentials = false;
				smtp.Credentials = new NetworkCredential(_mailSettings.UserName, _mailSettings.Password);
				MailMessage mailMessage = new MailMessage();
				mailMessage.From = new MailAddress(_mailSettings.SenderAddress);
				mailMessage.To.Add(_mailSettings.ToAddress);
				mailMessage.Body = body;
				mailMessage.Subject = subject;
				await smtp.SendMailAsync(mailMessage);
			}
		}
	}

	public interface IMailService {
		public void SendMail(string subject, string body);
	}
}
