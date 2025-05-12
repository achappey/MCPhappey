using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DataFormats.WebPages;

namespace MCPhappey.Core.Extensions;

public static class AspNetCoreExtensions
{
    public static IServiceCollection AddMcpCoreServices(
        this WebApplicationBuilder builder,
        List<ServerConfig> servers,
        Dictionary<string, Dictionary<string, string>>? domainHeaders = null)
    {
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddSingleton<TransformService>();
        builder.Services.AddSingleton<DownloadService>();
        builder.Services.AddSingleton<PromptService>();
        builder.Services.AddSingleton<SamplingService>();
        builder.Services.AddScoped<UploadService>();
        builder.Services.AddSingleton<ResourceService>();

        builder.Services.AddSingleton<IReadOnlyList<ServerConfig>>(servers);
        builder.Services.AddSingleton<WebScraper>();

        builder.Services.AddHttpClient();
        builder.Services.AddLogging();
        builder.Services.AddKernel();

        builder.Services.AddMcpServer()
            .WithConfigureSessionOptions(servers);

        return builder.Services;
    }
}