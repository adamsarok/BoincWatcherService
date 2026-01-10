using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register TableServiceClient
builder.Services.AddSingleton(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING") 
        ?? throw new InvalidOperationException("STORAGE_CONNECTION_STRING not found");
    return new TableServiceClient(connectionString);
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
