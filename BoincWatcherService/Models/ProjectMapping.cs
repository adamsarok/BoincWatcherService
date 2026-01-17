using System;
using System.ComponentModel.DataAnnotations;

namespace BoincWatcherService.Models;

public class ProjectMapping : Entity {
	[Required]
	public string ProjectName { get; set; }
	[Required]
	public string ProjectUrl { get; set; }
	[Required]
	public Guid ProjectId { get; set; } = Guid.NewGuid();
	public virtual Project Project { get; set; }
}
