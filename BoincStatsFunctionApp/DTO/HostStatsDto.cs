using Azure;
using Azure.Data.Tables;

namespace BoincStatsFunctionApp.DTO;

public class HostStatsDto : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public string HostName { get; set; } = string.Empty;
	public long TotalCredit { get; set; } = 0;
	public long RAC { get; set; } = 0;
	public DateTimeOffset? Timestamp { get; set; } = DateTimeOffset.UtcNow;
	public ETag ETag { get; set; } = ETag.All;
	public DateTimeOffset LastTaskCompletedTimestamp { get; set; } = DateTimeOffset.MinValue;
}
