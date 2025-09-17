using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class AgentRegistrationService : BackgroundService
{
    private readonly ILogger<AgentRegistrationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _registerWebhookUrl;
    private readonly string _agentWebhookUrl;

    public AgentRegistrationService(
        ILogger<AgentRegistrationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        // Set these URLs appropriately for your environment
        _registerWebhookUrl = "https://localhost:7071/api/registerwebhook";
        _agentWebhookUrl = "https://localhost:5001/api/agentwebhook";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(_agentWebhookUrl), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_registerWebhookUrl, content, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Agent registered successfully.");
            }
            else
            {
                _logger.LogWarning("Agent registration failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering agent webhook.");
        }
    }
}