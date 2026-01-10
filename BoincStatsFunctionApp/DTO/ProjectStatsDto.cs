using Azure;
using Azure.Data.Tables;

namespace BoincStatsFunctionApp.DTO;

public class ProjectStatsTableEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;
	public double TotalCredit { get; set; } = 0;
	public double RAC { get; set; } = 0;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
	public DateTimeOffset LastTaskCompletedTimestamp { get; set; } = DateTimeOffset.MinValue;
}
