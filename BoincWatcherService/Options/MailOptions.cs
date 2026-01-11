using System.Collections.Generic;
using System.Text.Json.Serialization;
using static BoincWatchService.Services.HostState;

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
	public string CronSchedule { get; set; } = "0 0 * * * ?";
	public List<HostStates> SendNotificationOnStates { get; set; }
}
