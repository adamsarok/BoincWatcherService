using BoincWatchService.Jobs;
using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
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
					services.Configure<FunctionAppOptions>(hostContext.Configuration.GetSection("FunctionAppSettings"));

					services.AddSingleton<IBoincService, BoincService>();
					services.AddSingleton<IMailService, MailService>();
					services.AddHttpClient<IFunctionAppService, FunctionAppService>();

					services.AddQuartz(q => {
						var mailOptions = hostContext.Configuration.GetSection("MailSettings").Get<MailOptions>();
						var functionAppOptions = hostContext.Configuration.GetSection("FunctionAppSettings").Get<FunctionAppOptions>();

						if (mailOptions?.IsEnabled == true) {
							var mailJobKey = new JobKey("MailNotificationJob");
							q.AddJob<MailNotificationJob>(opts => opts.WithIdentity(mailJobKey));
							q.AddTrigger(opts => opts
								.ForJob(mailJobKey)
								.WithIdentity("MailNotificationJob-trigger")
								.WithCronSchedule(mailOptions.CronSchedule ?? "0 0 0 * * ?"));
						}

						if (functionAppOptions?.IsEnabled == true) {
							var functionAppJobKey = new JobKey("FunctionAppUploadJob");
							q.AddJob<FunctionAppUploadJob>(opts => opts.WithIdentity(functionAppJobKey));
							q.AddTrigger(opts => opts
								.ForJob(functionAppJobKey)
								.WithIdentity("FunctionAppUploadJob-trigger")
								.WithCronSchedule(functionAppOptions.CronSchedule ?? "0 0 0 * * ?"));
						}
					});

					services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
				});
	}
}
