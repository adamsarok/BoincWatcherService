using System;

namespace BoincWatcherService.Models;

public class Project {
	public Guid ProjectId { get; set; } = Guid.NewGuid();
	public string ProjectNameDisplay { get; set; }
}
