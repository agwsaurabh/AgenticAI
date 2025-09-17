using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults();

builder.ConfigureServices(services =>
{
    services.AddHostedService<AgentRegistrationService>();
    services.AddHostedService<ContextCreationAgentService>();
    services.AddHttpClient();
    services.AddSingleton<IContextRetriever, ContextRetriever>(); // Added retriever
});

var host = builder.Build();
host.Run();
