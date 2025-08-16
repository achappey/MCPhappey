using MCPhappey.Auth.Extensions;
using MCPhappey.Common;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using OpenAI;

namespace MCPhappey.Tools.OpenAI.Containers;

public class ContainerScraper : IContentScraper
{

    public bool SupportsHost(ServerConfig serverConfig, string url)
        => url.StartsWith(ContainerExtensions.BASE_URL, StringComparison.OrdinalIgnoreCase);

    public async Task<IEnumerable<FileItem>?> GetContentAsync(IMcpServer mcpServer,
        IServiceProvider serviceProvider, string url, CancellationToken cancellationToken = default)
    {
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();
        var userId = serviceProvider.GetUserId();
        var client = openAiClient
                    .GetContainerClient();


        if (url.StartsWith($"{ContainerExtensions.BASE_URL}/", StringComparison.OrdinalIgnoreCase)
            && url.EndsWith("/files", StringComparison.OrdinalIgnoreCase))
        {
            string? vectorStoreId = null;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var segs = uri.AbsolutePath.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segs.Length >= 4 && segs[1] == "containers" && segs[^1] == "files")
                    vectorStoreId = segs[2];
            }

            var list = await client.GetContainerFilesAsync(vectorStoreId).ToAsyncEnumerable().ToListAsync(cancellationToken: cancellationToken);

            return list.Select(a => a.ToFileItem(url));
        }

        return [];
    }

}
