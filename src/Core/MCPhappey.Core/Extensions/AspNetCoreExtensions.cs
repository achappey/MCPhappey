using MCPhappey.Common;
using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DataFormats.WebPages;

namespace MCPhappey.Core.Extensions;

public static class AspNetCoreExtensions
{
    public static IServiceCollection AddMcpCoreServices(
        this IServiceCollection services,
        List<ServerConfig> servers)
    {
        services.AddHttpContextAccessor();

        services.AddSingleton<TransformService>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<PromptService>();
        services.AddSingleton<SamplingService>();
        services.AddScoped<UploadService>();
        services.AddSingleton<ResourceService>();

        services.AddSingleton<IReadOnlyList<ServerConfig>>(servers);
        services.AddSingleton<WebScraper>();
        services.AddScoped<HeaderProvider>();

        services.AddHttpClient();
        services.AddLogging();
        services.AddKernel();

        services.AddMcpServer()
            .WithConfigureSessionOptions(servers);

        return services;
    }
}