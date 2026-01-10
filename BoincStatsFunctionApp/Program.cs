using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register TableServiceClient - use connection string for local dev, managed identity for production
builder.Services.AddSingleton(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    
    // If using local Azurite or connection string is provided
    if (!string.IsNullOrEmpty(connectionString) && 
        (connectionString.Contains("UseDevelopmentStorage=true") || connectionString.Contains("AccountKey=")))
    {
        return new TableServiceClient(connectionString);
    }
    
    // Otherwise use managed identity with Azure Storage
    var tableServiceUri = Environment.GetEnvironmentVariable("AzureWebJobsStorage__tableServiceUri") 
        ?? throw new InvalidOperationException("AzureWebJobsStorage__tableServiceUri environment variable not found");
    
    var clientId = Environment.GetEnvironmentVariable("AzureWebJobsStorage__clientId");
    var credential = string.IsNullOrEmpty(clientId) 
        ? new DefaultAzureCredential() 
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId });
    
    return new TableServiceClient(new Uri(tableServiceUri), credential);
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
