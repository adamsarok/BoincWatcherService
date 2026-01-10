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
    var tableServiceUri = Environment.GetEnvironmentVariable("AzureWebJobsStorage__tableServiceUri") 
        ?? throw new InvalidOperationException("AzureWebJobsStorage__tableServiceUri environment variable not found");
    
    return new TableServiceClient(new Uri(tableServiceUri), new DefaultAzureCredential(new DefaultAzureCredentialOptions {
        ManagedIdentityClientId = Environment.GetEnvironmentVariable("AzureWebJobsStorage__clientId")
    }));
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
