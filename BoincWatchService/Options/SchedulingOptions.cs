using System.Collections.Generic;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService.Options;

public class SchedulingOptions {
	public int ScheduleIntervalMinutes { get; set; }
	public List<HostStates> SendNotificationOnStates { get; set; }
}
