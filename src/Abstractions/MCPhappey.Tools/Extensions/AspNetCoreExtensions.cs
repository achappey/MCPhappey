using MCPhappey.Common;
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

        return builder;
    }


}
