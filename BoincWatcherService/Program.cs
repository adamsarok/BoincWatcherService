using BoincWatchService.Data;
using BoincWatchService.Jobs;
using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using System;
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
				services.Configure<DatabaseOptions>(hostContext.Configuration.GetSection("DatabaseSettings"));
				services.Configure<FunctionAppOptions>(hostContext.Configuration.GetSection("FunctionAppSettings"));

				// Register HttpClient
				services.AddHttpClient();

				// Register DbContext with PostgreSQL
				services.AddDbContext<StatsDbContext>(options => {
					var connectionString = hostContext.Configuration.GetConnectionString("BoincWatcher");
					if (string.IsNullOrEmpty(connectionString)) {
						throw new InvalidOperationException("BoincWatcher connection string not found");
					}

					options.UseNpgsql(connectionString, npgsqlOptions => {
						npgsqlOptions.EnableRetryOnFailure(
							maxRetryCount: 5,
							maxRetryDelay: TimeSpan.FromSeconds(30),
							errorCodesToAdd: null);
					});
				});

				services.AddSingleton<IBoincService, BoincService>();
				services.AddSingleton<IMailService, MailService>();
				services.AddScoped<IStatsService, StatsService>();

				// Register data initialization service
				services.AddHostedService<DataInitializationService>();

				services.AddQuartz(q => {
					var mailOptions = hostContext.Configuration.GetSection("MailSettings").Get<MailOptions>();
					var databaseOptions = hostContext.Configuration.GetSection("DatabaseSettings").Get<DatabaseOptions>();

					if (mailOptions?.IsEnabled == true) {
						var mailJobKey = new JobKey("MailNotificationJob");
						q.AddJob<MailNotificationJob>(opts => opts.WithIdentity(mailJobKey));
						q.AddTrigger(opts => opts
							.ForJob(mailJobKey)
							.WithIdentity("MailNotificationJob-trigger")
							.WithCronSchedule(mailOptions.CronSchedule ?? "0 0 0 * * ?"));
					}

					if (databaseOptions?.IsEnabled == true) {
						var statsJobKey = new JobKey("StatsUploadJob");
						q.AddJob<FunctionAppUploadJob>(opts => opts.WithIdentity(statsJobKey));

						// Trigger on schedule
						q.AddTrigger(opts => opts
							.ForJob(statsJobKey)
							.WithIdentity("StatsUploadJob-trigger")
							.WithCronSchedule(databaseOptions.CronSchedule ?? "0 0 0 * * ?"));
					}
				});

				services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
			});
	}
}
