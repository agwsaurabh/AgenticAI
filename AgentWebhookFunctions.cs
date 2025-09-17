using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class AgentWebhookFunctions
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AgentWebhookFunctions> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private record WebhookNotification(string ContextId, string ContextUrl, DateTime Timestamp);

    public AgentWebhookFunctions(IHttpClientFactory httpClientFactory, ILogger<AgentWebhookFunctions> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [Function("AgentWebhook")]
    public async Task<HttpResponseData> AgentWebhookAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        FunctionContext ctx)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation("Received raw webhook body: {Body}", body);

        WebhookNotification? notification = null;
        try
        {
            notification = JsonSerializer.Deserialize<WebhookNotification>(body, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize webhook payload.");
        }

        string? fetchedContext = null;
        int? statusCode = null;

        if (notification != null && !string.IsNullOrWhiteSpace(notification.ContextUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var resp = await client.GetAsync(notification.ContextUrl);
                statusCode = (int)resp.StatusCode;
                fetchedContext = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Fetched context (ContextId={ContextId}) Status={Status} BodyLength={Len}",
                        notification.ContextId, resp.StatusCode, fetchedContext?.Length);
                }
                else
                {
                    _logger.LogWarning("Failed to fetch context (ContextId={ContextId}) Status={Status} Body={Body}",
                        notification.ContextId, resp.StatusCode, fetchedContext);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching context from {Url}", notification.ContextUrl);
            }
        }
        else
        {
            _logger.LogWarning("Webhook notification missing contextUrl or failed to parse.");
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        var result = new
        {
            received = notification,
            fetchStatus = statusCode,
            context = fetchedContext
        };
        await response.WriteStringAsync(JsonSerializer.Serialize(result));
        return response;
    }
}