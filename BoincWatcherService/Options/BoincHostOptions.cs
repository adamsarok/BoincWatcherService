using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BoincWatchService.Options;

public class BoincHostOptions {
	[Required]
	public string IP { get; set; } 

	public int Port { get; set; }

	[Required]
	[JsonIgnore]
	public string Password { get; set; }
}
