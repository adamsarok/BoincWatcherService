using System;

namespace BoincWatcherService.Models;

public class BoincApp {
	public string ProjectName { get; set; } = string.Empty;
	public string ProjectUrl { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string UserFriendlyName { get; set; } = string.Empty;
	public DateTime UpdatedAt { get; set; }
}
