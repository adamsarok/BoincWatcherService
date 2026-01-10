using Azure;
using Azure.Data.Tables;

namespace Common.Models;

public class ProjectStatsTableEntity : ITableEntity {
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public double TotalCredit { get; set; } = 0;
	public double RAC { get; set; } = 0;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
	public DateTimeOffset? LatestTaskDownloadTime { get; set; } = null;
}
