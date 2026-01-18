using BoincWatcherService.Models;
using BoincWatchService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatchService.Services;

public class DataInitializationService : IHostedService {
	private readonly ILogger<DataInitializationService> _logger;
	private readonly IServiceProvider _serviceProvider;

	public DataInitializationService(
		ILogger<DataInitializationService> logger,
		IServiceProvider serviceProvider) {
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

	public async Task StartAsync(CancellationToken cancellationToken) {
		using var scope = _serviceProvider.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<StatsDbContext>();

		try {
			_logger.LogInformation("Checking if database initialization is needed...");

			// Check if ProjectStats table is empty
			var hasData = await dbContext.ProjectStats.AnyAsync(cancellationToken);

			if (!hasData) {
				_logger.LogInformation("ProjectStats table is empty. Starting data import from init_stats.csv...");
				await ImportInitialStatsAsync(dbContext, cancellationToken);
			} else {
				_logger.LogInformation("ProjectStats table already contains data. Skipping initialization.");
			}
		} catch (Exception ex) {
			_logger.LogError(ex, "Error during data initialization");
			// Don't stop the application, just log the error
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		return Task.CompletedTask;
	}

	private static string[] ParseCsvLine(string line) {
		var result = new List<string>();
		var currentField = new System.Text.StringBuilder();
		var insideQuotes = false;

		for (int i = 0; i < line.Length; i++) {
			var c = line[i];

			if (c == '"') {
				insideQuotes = !insideQuotes;
			} else if (c == ',' && !insideQuotes) {
				result.Add(currentField.ToString().Trim());
				currentField.Clear();
			} else {
				currentField.Append(c);
			}
		}

		// Add the last field
		result.Add(currentField.ToString().Trim());

		return result.ToArray();
	}

	private async Task ImportInitialStatsAsync(StatsDbContext dbContext, CancellationToken cancellationToken) {
		var csvPath = "init_stats.csv";

		if (!File.Exists(csvPath)) {
			_logger.LogWarning("init_stats.csv file not found at path: {Path}", Path.GetFullPath(csvPath));
			return;
		}

		try {
			var lines = await File.ReadAllLinesAsync(csvPath, cancellationToken);
			var timestamp = DateTimeOffset.UtcNow;
			var importedCount = 0;

			// Skip header line
			foreach (var line in lines.Skip(1)) {
				if (string.IsNullOrWhiteSpace(line)) {
					continue;
				}

				var parts = ParseCsvLine(line);
				if (parts.Length < 2) {
					_logger.LogWarning("Invalid line in CSV: {Line}", line);
					continue;
				}

				var projectName = parts[0];
				var pointsText = parts[1].Replace(",", "");

				if (!double.TryParse(pointsText, NumberStyles.Float, CultureInfo.InvariantCulture, out var totalCredit)) {
					_logger.LogWarning("Could not parse points for project {ProjectName}: {Points}", projectName, parts[1]);
					continue;
				}

				var projectStats = new ProjectStats {
					YYYYMMDD = "19990101",
					ProjectName = projectName,
					TotalCredit = totalCredit,
					LatestTaskDownloadTime = null
				};

				dbContext.ProjectStats.Add(projectStats);
				importedCount++;
			}

			await dbContext.SaveChangesAsync(cancellationToken);
			_logger.LogInformation("Successfully imported {Count} project stats from init_stats.csv", importedCount);
		} catch (Exception ex) {
			_logger.LogError(ex, "Error importing data from init_stats.csv");
			throw;
		}
	}
}
