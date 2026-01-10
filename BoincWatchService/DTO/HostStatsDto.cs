using System;

namespace BoincWatchService.DTO;

public class HostStatsDto {
	public string HostName { get; set; } = string.Empty;
	public long TotalCredit { get; set; } = 0;
	public long RAC { get; set; } = 0;
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset LastTaskCompletedTimestamp { get; set; } = DateTimeOffset.MinValue;
}
