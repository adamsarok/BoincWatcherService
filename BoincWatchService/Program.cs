using BoincWatchService.Options;
using BoincWatchService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace BoincWatchService {
	public class Program {
		public static void Main(string[] args) {
			CreateHostBuilder(args).Build().Run();
		}
		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((hostContext, config) => {
					// Explicitly add User Secrets in all environments
					config.AddUserSecrets<Program>();
				})
				.ConfigureServices((hostContext, services) => {
					services.Configure<List<BoincHostOptions>>(hostContext.Configuration.GetSection("BoincHosts"));
					services.Configure<MailOptions>(hostContext.Configuration.GetSection("MailSettings"));
					services.Configure<SchedulingOptions>(hostContext.Configuration.GetSection("SchedulingSettings"));

					services.AddHostedService<Worker>();
					services.AddSingleton<IBoincService, BoincService>();
					services.AddSingleton<IMailService, MailService>();
				});
	}
}
