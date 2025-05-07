
using MCPhappey.Core.Auth;
using MCPhappey.Core.Models.Protocol;
using MCPhappey.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.DataFormats.WebPages;

namespace MCPhappey.Core.Extensions;

public static class AspNetCoreExtensions
{
    public static IServiceCollection AddMcpCoreServices(
        this WebApplicationBuilder builder,
        List<ServerConfig> servers,
        Dictionary<string, Dictionary<string, string>>? domainHeaders = null)
    {
        builder.Services.AddAuthorization();

        builder.Services.AddSingleton<TransformService>();
        builder.Services.AddSingleton<DownloadService>();
        builder.Services.AddScoped<PromptService>();
        builder.Services.AddScoped<SamplingService>();
        builder.Services.AddScoped<UploadService>();
        builder.Services.AddScoped<ResourceService>();

        builder.Services.AddSingleton<IJwtValidator, JwtValidator>();

        if (domainHeaders != null)
        {
            builder.Services.AddSingleton(domainHeaders);
        }

        builder.Services.AddSingleton<IReadOnlyList<ServerConfig>>(servers);
        builder.Services.AddSingleton<WebScraper>();
        builder.Services.AddSingleton<MsExcelDecoder>();
        builder.Services.AddSingleton<MsWordDecoder>();
        builder.Services.AddSingleton<MsPowerPointDecoder>();
        builder.Services.AddSingleton<PdfDecoder>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddLogging();
        builder.Services.AddKernel();

        builder.Services.AddMcpServer()
            .WithConfigureSessionOptions(servers);

        return builder.Services;
    }
}