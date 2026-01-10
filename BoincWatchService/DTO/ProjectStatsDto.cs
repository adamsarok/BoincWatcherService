using System;

namespace BoincWatchService.DTO;

public class ProjectStatsDto {
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public string ProjectName { get; set; }
	public long TotalCredit { get; set; } = 0;
	public long RAC { get; set; } = 0;
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset LastTaskCompletedTimestamp { get; set; } = DateTimeOffset.MinValue;
}
