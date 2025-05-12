using System.Net.Mime;
using MCPhappey.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.DataFormats.WebPages;

namespace MCPhappey.Core.Extensions;

public static class KernelMemoryExtensions
{
    public static IServiceCollection AddKernelMemoryWithOptions(
        this IServiceCollection services,
        Action<IKernelMemoryBuilder> configure,
        KernelMemoryBuilderBuildOptions buildOptions)
    {
        // 1. Maak een nieuwe builder
        var builder = new KernelMemoryBuilder(services);

        // 2. Voer de configuratie uit
        configure(builder);

        // 3. Bouw met je eigen opties
        var memoryClient = builder.Build(buildOptions);

        // 4. Registreer de client
        services.AddSingleton(memoryClient);

        return services;
    }

    public static FileItem GetFileItemFromFileContent(this Microsoft.KernelMemory.DataFormats.FileContent file, string uri)
        => new()
        {
            Contents = BinaryData.FromString(string.Join("\\n\\n",
                file.Sections.Select(a => a.Content))),
            MimeType = MediaTypeNames.Text.Plain,
            Uri = uri,
        };

    public static FileItem ToFileItem(this WebScraperResult webScraperResult, string uri) => new()
    {
        Contents = webScraperResult.Content,
        MimeType = webScraperResult.ContentType,
        Uri = uri,
    };
}
