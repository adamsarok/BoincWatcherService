using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AdamSarok.BoincStatsFunctionApp;

public class Stats
{
    private readonly ILogger<Stats> _logger;

    public Stats(ILogger<Stats> logger)
    {
        _logger = logger;
    }

    [Function("Stats")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}