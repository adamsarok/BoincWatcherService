namespace BoincWatchService.Options;

public class SchedulingOptions {
	public string StatsSchedule { get; set; } = "0 0 * * * ?";
	public string BoincTaskSchedule { get; set; } = "0 0/5 * * * ?";
}
