namespace BoincWatchService.Options;

public class DatabaseOptions {
	public bool IsEnabled { get; set; } = false;
	public string CronSchedule { get; set; } = "0 0 * * * ?";
}
