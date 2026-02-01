using Azure;
using Azure.Data.Tables;

namespace Common.Models;

public class EfficiencyTableEntity : ITableEntity {
	public EfficiencyTableEntity() { }
	public EfficiencyTableEntity(string hostName, string projectName) {
		PartitionKey = hostName;
		RowKey = projectName;
	}
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public double CPUHoursTotal { get; set; } = 0;
	public double PointsTotal { get; set; } = 0;
	public double PointsPerCPUHour { get; set; } = 0;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}
