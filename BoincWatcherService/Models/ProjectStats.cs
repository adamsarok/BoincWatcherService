using System;

namespace BoincWatcherService.Models;

public class ProjectStats : Entity {
	public string YYYYMMDD { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;
	public double TotalCredit { get; set; } = 0;
	public DateTimeOffset? LatestTaskDownloadTime { get; set; }
}
