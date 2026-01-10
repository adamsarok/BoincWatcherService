using System;

namespace BoincWatchService.DTO;

public class HostStatsDto {
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public string HostName { get; set; } = string.Empty;
	public double TotalCredit { get; set; } = 0;
	public double RAC { get; set; } = 0;
	public DateTimeOffset LastTaskCompletedTimestamp { get; set; } = DateTimeOffset.MinValue;
}
