using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using Eia.Connector.Services;
using Eia.Connector.Exceptions;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var apiKey = Environment.GetEnvironmentVariable("EIA_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("The EIA_API_KEY environment variable is not configured");
    Console.ResetColor();
    Environment.Exit(1);
}

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

services.AddLogging(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddConsole();
});

var retryCount = configuration.GetValue<int>("EiaConnector:RetryCount", 2);
var timeout = configuration.GetValue<int>("EiaConnector:TimeoutSeconds", 30);

services.AddHttpClient<EiaNuclearOutagesConnector>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(timeout);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddPolicyHandler(BuildRetryPolicy(retryCount));

var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILogger<EiaNuclearOutagesConnector>>();

try
{
    logger.LogInformation("EIA Nuclear Outages Connector starting...");
    logger.LogInformation("BaseUrl: {Url}", configuration["EiaConnector:BaseUrl"]);
    logger.LogInformation("PageSize: {Size}", configuration["EiaConnector:PageSize"]);

    var connector = provider.GetRequiredService<EiaNuclearOutagesConnector>();

    var count = await connector.ExtractAsync();

    logger.LogInformation("Process finished — {Count} valid records saved to Parquet", count);
}
catch (EiaAuthException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"\n[ERROR] Invalid Credentials: {ex.Message}\n");
    Console.ResetColor();
    Environment.Exit(2);
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Unexpected error — the connector has stopped");
    Environment.Exit(3);
}

static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy(int retryCount) =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: retryCount,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, delay, attempt, _) =>
                Console.WriteLine(
                    $"[Polly] Retry {attempt}/{retryCount} " +
                    $"in {delay.TotalSeconds}s — " +
                    $"Reason: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}"));