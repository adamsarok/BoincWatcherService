using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using static BoincWatchService.Services.HostState;

namespace BoincWatchService.Services {
	public class ConfigService : IConfigService {
		private readonly ILogger<Worker> _logger;
		public ConfigService(ILogger<Worker> logger) {
			_logger = logger;
		}
		public List<HostSettings> GetHosts() {
			List<HostSettings> confs = new List<HostSettings>();
			var env = TryGetEnviromentVariable("BOINC_HOSTS");
			if (string.IsNullOrWhiteSpace(env)) return confs;
			var split = env.Split(';');
			int i = 0;
			while (i < split.Length) {
				confs.Add(new HostSettings() {
					IP = split[i++],
					Port = int.Parse(split[i++]),
					Password = split[i++]
				});
			}
			return confs;
		}
		public MailSettings GetMailSettings() {
			return new MailSettings() {
				SmtpHost = TryGetEnviromentVariable("SMTP_HOST"),
				Password = TryGetEnviromentVariable("MAIL_PASSWORD"),
				UserName = TryGetEnviromentVariable("MAIL_USERNAME"),
				SenderAddress = TryGetEnviromentVariable("SENDER_ADDRESS"),
				ToAddress = TryGetEnviromentVariable("TO_ADDRESS")
			};
		}
		public SchedulingSettings GetSchedulingSettings() {
			var result = new SchedulingSettings() {
				ScheduleIntervalMinutes = int.Parse(TryGetEnviromentVariable("SCHEDULE_INTERVAL_MINUTES")),
				SendClientStates = new List<HostStates>()
			};
			foreach (var state in TryGetEnviromentVariable("SEND_CLIENT_STATES").Split(';')) {
				switch (state.ToUpper()) {
					case "DOWN":
						result.SendClientStates.Add(HostStates.Down);
						break;
					case "NORUNNINGTASKS":
						result.SendClientStates.Add(HostStates.NoRunningTasks);
						break;
					case "NOTASKS":
						result.SendClientStates.Add(HostStates.NoTasks);
						break;
					case "OK":
						result.SendClientStates.Add(HostStates.OK);
						break;
				}
			}
			return result;
		}
		private string TryGetEnviromentVariable(string key) {
			var env = Environment.GetEnvironmentVariable(key);
			if (string.IsNullOrWhiteSpace(env)) {
				_logger.LogError($"Enviroment variable {key} not defined");
				throw new Exception($"Enviroment variable {key} not defined");
			}
			return env;
		}
		private bool TryParseBool(string value) {
			if (value.ToUpper().Contains('Y') || value.ToUpper().Contains('1') || value.ToUpper().Contains('T')) return true;
			return false;
		}
	}
	public class HostSettings {
		public string IP { get; set; }
		public int Port { get; set; }
		public string Password { get; set; }
	}
	public class MailSettings {
		public string SmtpHost { get; set; }
		public int SmtpPort { get; set; }
		public string SenderAddress { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public string ToAddress { get; set; }
	}
	public class SchedulingSettings {
		public int ScheduleIntervalMinutes { get; set; }
		public List<HostStates> SendClientStates { get; set; }
	}
	public interface IConfigService {
		List<HostSettings> GetHosts();
		MailSettings GetMailSettings();
		SchedulingSettings GetSchedulingSettings();
	}
}
