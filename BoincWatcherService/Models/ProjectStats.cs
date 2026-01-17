using System;
using System.ComponentModel.DataAnnotations;

namespace BoincWatcherService.Models;

public class ProjectStats : Entity {
	[Required]
	public string YYYYMMDD { get; set; }
	public Guid ProjectId { get; set; }
	[Required]
	public string MasterUrl { get; set; }
	[Required]
	public string ProjectName { get; set; }
	public double TotalCredit { get; set; } = 0;
	public DateTimeOffset? LatestTaskDownloadTime { get; set; }
}
