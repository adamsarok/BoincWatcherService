using System;

namespace BoincWatcherService.Models;

public class HostStats : Entity {

	public string YYYYMMDD { get; set; } = string.Empty;
	public string HostName { get; set; } = string.Empty;
	public double TotalCredit { get; set; } = 0;
	public DateTimeOffset? Timestamp { get; set; }
	public DateTimeOffset? LatestTaskDownloadTime { get; set; }
}
