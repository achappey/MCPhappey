using Microsoft.Extensions.DependencyInjection;

namespace MCPhappey.Tools.Tavily;

public static class TavilyServiceCollectionExtensions
{
    public static IServiceCollection AddTavily(this IServiceCollection services, string apiKey)
    {
        // Prefer config section; fall back to env var TAVILY_API_KEY

        services.AddSingleton<ITavilyClient, TavilyClient>((sp) =>
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Tavily API key is missing. Set Tavily:ApiKey or TAVILY_API_KEY.");

            return new TavilyClient(sp.GetRequiredService<IHttpClientFactory>(), apiKey);
        });

        return services;
    }
}
