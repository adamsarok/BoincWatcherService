using Azure;
using Azure.Data.Tables;

namespace BoincStatsFunctionApp.DTO;

public class HostStatsTableEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public string HostName { get; set; } = string.Empty;
	public double TotalCredit { get; set; } = 0;
	public double RAC { get; set; } = 0;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
	public DateTimeOffset LastTaskCompletedTimestamp { get; set; } = DateTimeOffset.MinValue;
}
