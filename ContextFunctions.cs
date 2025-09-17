using System.Collections.Concurrent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;

public class ContextFunctions
{
    // In-memory store (ContextId -> Payload)
    private static readonly ConcurrentDictionary<string, string> _contextStore = new();

    [Function("CreateContextLink")]
    public HttpResponseData CreateContextLink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var body = new StreamReader(req.Body).ReadToEnd();
        var contextId = Guid.NewGuid().ToString();

        _contextStore[contextId] = body;

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        response.WriteString($"https://myfuncapp.azurewebsites.net/api/getcontext/{contextId}");

        return response;
    }

    [Function("GetContext")]
    public HttpResponseData GetContext(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getcontext/{contextId}")] HttpRequestData req,
        string contextId)
    {
        var response = req.CreateResponse();

        if (_contextStore.TryGetValue(contextId, out var state))
        {
            response.StatusCode = HttpStatusCode.OK;
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(state);
        }
        else
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.WriteString("Context not found");
        }

        return response;
    }
}
