using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class AgentWebhookFunctions
{
    [Function("AgentWebhook")]
    public async Task<HttpResponseData> AgentWebhookAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        FunctionContext ctx)
    {
        var logger = ctx.GetLogger("AgentWebhook");
        string body = await new StreamReader(req.Body).ReadToEndAsync();

        // Log receipt of webhook (the body contains contextId, contextUrl, timestamp)
        logger.LogInformation("Received webhook notification: {Body}", body);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Webhook received.");
        return response;
    }
}