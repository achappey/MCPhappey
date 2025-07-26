using MCPhappey.Common;
using MCPhappey.Tools.GitHub.RestCountries;
using MCPhappey.Tools.Graph;
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
