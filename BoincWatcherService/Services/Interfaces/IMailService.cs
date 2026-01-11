using System.Threading.Tasks;

namespace BoincWatchService.Services.Interfaces;

public interface IMailService {
	public Task SendMail(string subject, string body);
}
