using BoincWatcherService.Data;
using BoincWatcherService.Services;
using BoincWatcherService.Services.Interfaces;
using BoincWatchService.Data;
using BoincWatchService.Jobs;
using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BoincWatchService {
	public class Program {
		public static void Main(string[] args) {
			var host = CreateHostBuilder(args).Build();

			// Apply pending migrations on startup
			using (var scope = host.Services.CreateScope()) {
				var services = scope.ServiceProvider;
				try {
					var context = services.GetRequiredService<StatsDbContext>();
					context.Database.Migrate();
				} catch (Exception ex) {
					Console.WriteLine($"An error occurred while migrating the database: {ex.Message}");
					throw;
				}
			}

			host.Run();
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
				services.Configure<Options.SchedulingOptions>(hostContext.Configuration.GetSection("SchedulingOptions"));
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
					options.AddInterceptors(new EntityInterceptor());

				});

				services.AddSingleton<IRpcClientFactory, RpcClientFactory>();
				services.AddSingleton<IBoincService, BoincService>();
				services.AddSingleton<IMailService, MailService>();
				services.AddScoped<IStatsService, StatsService>();
				services.AddScoped<IFunctionAppService, FunctionAppService>();

				services.AddFeatureManagement();

				services.AddQuartz(q => {
					var mailOptions = hostContext.Configuration.GetSection("MailSettings").Get<MailOptions>();
					var schedulingOptions = hostContext.Configuration.GetSection("SchedulingOptions").Get<Options.SchedulingOptions>();

					var mailJobKey = new JobKey("MailNotificationJob");
					q.AddJob<MailNotificationJob>(opts => opts.WithIdentity(mailJobKey));
					q.AddTrigger(opts => opts
						.ForJob(mailJobKey)
						.WithIdentity("MailNotificationJob-trigger")
						.WithCronSchedule(mailOptions?.CronSchedule ?? "0 0 0 * * ?"));


					var statsJobKey = new JobKey("StatsUploadJob");
					q.AddJob<StatsJob>(opts => opts.WithIdentity(statsJobKey));

					var tasksJobKey = new JobKey("BoincTaskJob");
					q.AddJob<BoincTaskJob>(opts => opts.WithIdentity(tasksJobKey));

					if (schedulingOptions is null) {
						throw new InvalidOperationException("SchedulingOptions is not configured.");
					}

					// Immediate trigger for debugging
					if (Debugger.IsAttached) {
						q.AddTrigger(opts => opts
							.ForJob(statsJobKey)
							.WithIdentity($"{statsJobKey}-immediate-trigger")
							.StartNow());
						q.AddTrigger(opts => opts
							.ForJob(tasksJobKey)
							.WithIdentity($"{tasksJobKey}-immediate-trigger")
							.StartNow());
					}


					// Trigger on schedule
					q.AddTrigger(opts => opts
						.ForJob(statsJobKey)
						.WithIdentity($"{statsJobKey}-trigger")
						.WithCronSchedule(schedulingOptions.StatsSchedule ?? "0 0 0 * * ?"));
					q.AddTrigger(opts => opts
						.ForJob(tasksJobKey)
						.WithIdentity($"{tasksJobKey}-trigger")
						.WithCronSchedule(schedulingOptions.BoincTaskSchedule ?? "0 0/5 * * * ?"));

				});

				services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
			});
	}
}
