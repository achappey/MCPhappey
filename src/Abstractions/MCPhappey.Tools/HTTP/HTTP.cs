using System.ComponentModel;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.HTTP;

public static class HTTPService
{
    [Description("Fetches a public accessible url")]
    [McpServerTool(Title = "Fetch public URL",
        Destructive = false,
        ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> Http_FetchUrl(
        [Description("The url to fetch.")]
        string url,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        var content = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, url,
            cancellationToken) ?? throw new Exception();

        return content.ToContentBlocks();
    }
}

