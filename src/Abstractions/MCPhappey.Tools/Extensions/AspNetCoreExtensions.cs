using MCPhappey.Common;
using MCPhappey.Tools.GitHub.RestCountries;
using MCPhappey.Tools.Graph;
using MCPhappey.Tools.OpenAI.Files;
using MCPhappey.Tools.OpenAI.VectorStores;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MCPhappey.Simplicate.Extensions;

public static class AspNetCoreExtensions
{
    public static WebApplicationBuilder WithCompletion(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IAutoCompletion, GraphCompletion>();
        builder.Services.AddSingleton<IAutoCompletion, CountryCompletion>();
        builder.Services.AddSingleton<IContentScraper, VectorStoreScraper>();
        builder.Services.AddSingleton<IContentScraper, OpenAIFilesScraper>();

        return builder;
    }

    public static WebApplicationBuilder WithGoogleAI(
            this WebApplicationBuilder builder,
            string apiKey)
    {
        Mscc.GenerativeAI.GoogleAI googleAI = new(apiKey);
        builder.Services.AddSingleton(googleAI);

        return builder;
    }
}
