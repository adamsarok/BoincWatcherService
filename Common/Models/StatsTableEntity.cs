using Azure;
using Azure.Data.Tables;

namespace Common.Models;

public class StatsTableEntity : ITableEntity {
	public const string HOST_STATS = "HostStats";
	public const string PROJECT_STATS = "ProjectStats";
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public double CreditTotal { get; set; } = 0;
	public double CreditToday { get; set; } = 0;
	public double CreditThisWeek { get; set; } = 0;
	public double CreditThisMonth { get; set; } = 0;
	public double CreditThisYear { get; set; } = 0;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
	public DateTimeOffset? LatestTaskDownloadTime { get; set; } = null;
}
