using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class ContextFunctions
{
    // In-memory storage for contexts
    private static readonly ConcurrentDictionary<string, string> _contextStore = new();

    // In-memory list of registered webhook subscribers
    private static readonly ConcurrentBag<string> _webhookSubscribers = new();

    private readonly HttpClient _httpClient = new();

    public class CreateContextRequest
    {
        public string Payload { get; set; }
    }

    [Function("CreateContextLink")]
    public async Task<HttpResponseData> CreateContextLink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var requestData = JsonSerializer.Deserialize<CreateContextRequest>(requestBody);

        if (requestData == null || string.IsNullOrWhiteSpace(requestData.Payload))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid request: 'payload' is required.");
            return badResponse;
        }

        var contextId = Guid.NewGuid().ToString();
        _contextStore[contextId] = requestData.Payload;

        var baseUri = $"{req.Url.Scheme}://{req.Url.Host}";
        if (!req.Url.IsDefaultPort)
            baseUri += $":{req.Url.Port}";
        var contextUrl = $"{baseUri}/api/getcontext/{contextId}";

        var webhookPayload = new
        {
            contextId,
            contextUrl,
            timestamp = DateTime.UtcNow
        };

        var jsonPayload = new StringContent(JsonSerializer.Serialize(webhookPayload), Encoding.UTF8, "application/json");

        foreach (var subscriberUrl in _webhookSubscribers.Distinct())
        {
            try
            {
                await _httpClient.PostAsync(subscriberUrl, jsonPayload);
            }
            catch (Exception ex)
            {
                // Optionally log or handle errors here
            }
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await response.WriteStringAsync(contextUrl);
        return response;
    }

    [Function("GetContext")]
    public HttpResponseData GetContext(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getcontext/{contextId}")] HttpRequestData req,
        string contextId)
    {
        var response = req.CreateResponse();

        if (_contextStore.TryGetValue(contextId, out var payload))
        {
            response.StatusCode = HttpStatusCode.OK;
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(payload);
        }
        else
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.WriteString("Context not found");
        }

        return response;
    }

    [Function("RegisterWebhook")]
    public async Task<HttpResponseData> RegisterWebhook(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        string? webhookUrl = null;

        try
        {
            webhookUrl = JsonSerializer.Deserialize<string>(body);
        }
        catch
        {
            // fallback: treat body as raw URL string
            webhookUrl = body.Trim('\"');
        }

        var response = req.CreateResponse();

        if (string.IsNullOrWhiteSpace(webhookUrl) || !Uri.IsWellFormedUriString(webhookUrl, UriKind.Absolute))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteStringAsync("Invalid webhook URL.");
            return response;
        }

        _webhookSubscribers.Add(webhookUrl);

        response.StatusCode = HttpStatusCode.OK;
        await response.WriteStringAsync("Webhook registered successfully.");
        return response;
    }
}
