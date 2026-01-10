using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register TableServiceClient with managed identity
builder.Services.AddSingleton(sp =>
{
    var storageName = Environment.GetEnvironmentVariable("StorageName") 
        ?? throw new InvalidOperationException("StorageName environment variable not found");
    
    var tableServiceUri = new Uri($"https://{storageName}.table.core.windows.net/");
    return new TableServiceClient(tableServiceUri, new DefaultAzureCredential());
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
