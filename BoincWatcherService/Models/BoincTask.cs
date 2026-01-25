using System;

namespace BoincWatcherService.Models;

public class BoincTask {
	public string ProjectName { get; set; } = string.Empty;
	public string TaskName { get; set; } = string.Empty;
	public string HostName { get; set; } = string.Empty;
	public string AppName { get; set; } = string.Empty;
	public DateTime UpdatedAt { get; set; }
	public TimeSpan CurrentCpuTime { get; internal set; }
	public TimeSpan EstimatedCpuTimeRemaining { get; internal set; }
	public TimeSpan ElapsedTime { get; internal set; }
	public double FractionDone { get; internal set; }
	public DateTimeOffset ReceivedTime { get; internal set; }
}
