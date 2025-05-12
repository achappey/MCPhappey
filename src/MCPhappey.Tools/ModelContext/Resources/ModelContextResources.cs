using System.ComponentModel;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.ModelContext.Resources;

public static class ModelContextResourceService
{
    [Description("Reads a resource from the specified URI")]
    public static async Task<CallToolResponse> ReadResource(
        [Description("The URI of the resource to get. Must be a valid URI. Also supports links to authenticated content like SharePoint, Outlook, Simplicate, etc")]
        string uri,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(uri);

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var configs = serviceProvider.GetRequiredService<IReadOnlyList<ServerConfig>>();
        var config = configs.GetServerConfig(mcpServer);

        var content = await downloadService.GetContentAsync(config!, uri, null, cancellationToken) ?? throw new Exception();

        return content.ToReadResourceResult().ToCallToolResponse();
    }
}

