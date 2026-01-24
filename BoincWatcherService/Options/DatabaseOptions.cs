namespace BoincWatchService.Options;

public class DatabaseOptions {
	public string CronSchedule { get; set; } = "0 0 * * * ?";
}
