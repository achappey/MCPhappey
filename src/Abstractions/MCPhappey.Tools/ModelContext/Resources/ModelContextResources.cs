using System.ComponentModel;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.ModelContext.Resources;

public static class ModelContextResourceService
{
    [Description("Reads a resource from the specified URI")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<CallToolResponse> ReadResource(
        [Description("The URI of the resource to get. Must be a valid URI. Also supports links to authenticated content like SharePoint, Outlook, Simplicate, etc")]
        string uri,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(uri);

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        var content = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, uri, 
            cancellationToken) ?? throw new Exception();

        return content.ToReadResourceResult().ToCallToolResponse();
    }
}

