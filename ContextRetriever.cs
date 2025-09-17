using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public interface IContextRetriever
{
    Task<string?> GetContextAsync(string baseUrl, string contextId, CancellationToken cancellationToken = default);
}

public class ContextRetriever : IContextRetriever
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ContextRetriever> _logger;

    public ContextRetriever(IHttpClientFactory httpClientFactory, ILogger<ContextRetriever> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> GetContextAsync(string baseUrl, string contextId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL required", nameof(baseUrl));
        if (string.IsNullOrWhiteSpace(contextId))
            throw new ArgumentException("Context ID required", nameof(contextId));

        var url = $"{baseUrl.TrimEnd('/')}/api/getcontext/{contextId}";
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(url, cancellationToken);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("GetContextAsync failed: Status={Status} Url={Url} Body={Body}",
                    resp.StatusCode, url, body);
                return null;
            }

            _logger.LogInformation("Retrieved context {ContextId} (length {Len})", contextId, body.Length);
            return body;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception retrieving context {ContextId}", contextId);
            return null;
        }
    }
}