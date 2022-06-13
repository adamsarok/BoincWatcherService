using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BoincWatchService.Services {
	public class MailService :	IMailService {
		private readonly IConfigService _configService;
		public MailService(IConfigService configService) {
			_configService = configService;
		}
		public async void SendMail(string subject, string body) {
			var conf = _configService.GetMailSettings();
			using (var smtp = new SmtpClient(conf.SmtpHost, conf.SmtpPort)) {
				smtp.UseDefaultCredentials = false;
				smtp.Credentials = new NetworkCredential(conf.UserName, conf.Password);
				MailMessage mailMessage = new MailMessage();
				mailMessage.From = new MailAddress(conf.SenderAddress);
				mailMessage.To.Add(conf.ToAddress);
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
