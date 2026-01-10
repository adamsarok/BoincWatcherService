using System.Text.Json.Serialization;

namespace BoincWatchService.Options;

public class MailOptions {
	public string SmtpHost { get; set; }
	public int SmtpPort { get; set; }
	public string SenderAddress { get; set; }
	public string UserName { get; set; }

	[JsonIgnore]
	public string Password { get; set; }
	public string ToAddress { get; set; }
	public bool IsEnabled { get; set; }
}
