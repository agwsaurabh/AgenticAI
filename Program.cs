using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults();

builder.ConfigureServices(services =>
{
    services.AddHostedService<AgentRegistrationService>();
    services.AddHostedService<ContextCreationAgentService>(); // Added context creation agent
    services.AddHttpClient();
});

var host = builder.Build();

host.Run();
