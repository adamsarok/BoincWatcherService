namespace BoincWatchService.Options;

public class FunctionAppOptions {
	public string BaseUrl { get; set; } = string.Empty;
	public string FunctionKey { get; set; } = string.Empty;
	public bool IsEnabled { get; set; } = false;
	public string CronSchedule { get; set; } = "0 0 * * * ?";
}
