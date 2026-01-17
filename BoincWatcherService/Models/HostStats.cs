using System;
using System.ComponentModel.DataAnnotations;

namespace BoincWatcherService.Models;

public class HostStats : Entity {
	[Required]
	public string YYYYMMDD { get; set; }
	[Required]
	public string HostName { get; set; }
	public double TotalCredit { get; set; } = 0;
	public DateTimeOffset? Timestamp { get; set; }
	public DateTimeOffset? LatestTaskDownloadTime { get; set; }
}
