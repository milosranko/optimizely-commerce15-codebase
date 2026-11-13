using Commerce.Demo.Integrations.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Demo.Integrations.Startup;

public static class StartupExtensions
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services
            .AddHttpClient<ExampleClient>(client =>
            {
                client.BaseAddress = new Uri("https://example.com/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
