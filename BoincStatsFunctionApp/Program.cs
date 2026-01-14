using AdamSarok.BoincStatsFunctionApp.Data;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register DbContext with SQL Server and Managed Identity
builder.Services.AddDbContext<StatsDbContext>(options => {
	var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

	if (string.IsNullOrEmpty(connectionString)) {
		throw new InvalidOperationException("SqlConnectionString environment variable not found");
	}

	options.UseSqlServer(connectionString, sqlOptions => {
		sqlOptions.EnableRetryOnFailure(
			maxRetryCount: 5,
			maxRetryDelay: TimeSpan.FromSeconds(30),
			errorNumbersToAdd: null);
	});
});

// Configure SqlConnection with Managed Identity token
builder.Services.AddSingleton<Func<SqlConnection>>(sp => {
	return () => {
		var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

		if (string.IsNullOrEmpty(connectionString)) {
			throw new InvalidOperationException("SqlConnectionString environment variable not found");
		}

		var connection = new SqlConnection(connectionString);

		var credential = new DefaultAzureCredential();
		var token = credential.GetToken(
			new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }));
		connection.AccessToken = token.Token;

		return connection;
	};
});

builder.Services
	.AddApplicationInsightsTelemetryWorkerService()
	.ConfigureFunctionsApplicationInsights();

builder.Build().Run();

