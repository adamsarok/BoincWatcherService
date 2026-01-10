using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
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
					if (hostContext.HostingEnvironment.IsDevelopment()) {
						config.AddUserSecrets<Program>();
					}
				})
				.ConfigureServices((hostContext, services) => {
					services.Configure<List<BoincHostOptions>>(hostContext.Configuration.GetSection("BoincHosts"));
					services.Configure<MailOptions>(hostContext.Configuration.GetSection("MailSettings"));
					services.Configure<SchedulingOptions>(hostContext.Configuration.GetSection("SchedulingSettings"));
					services.Configure<FunctionAppOptions>(hostContext.Configuration.GetSection("FunctionAppSettings"));

					services.AddHostedService<Worker>();
					services.AddSingleton<IBoincService, BoincService>();
					services.AddSingleton<IMailService, MailService>();

					services.AddHttpClient<IFunctionAppService, FunctionAppService>();
				});
	}
}
