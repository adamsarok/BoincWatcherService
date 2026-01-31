using Azure;
using Azure.Data.Tables;

namespace Common.Models;

public class ResultsTableEntity : ITableEntity {
	public ResultsTableEntity(string hostName, string projectName, string appName) {
		PartitionKey = hostName;
		RowKey = $"{projectName}|{appName}";
	}
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public double CPUHoursTotal { get; set; } = 0;
	public double CPUHoursToday { get; set; } = 0;
	public double CPUHoursThisWeek { get; set; } = 0;
	public double CPUHoursThisMonth { get; set; } = 0;
	public double CPUHoursThisYear { get; set; } = 0;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}
