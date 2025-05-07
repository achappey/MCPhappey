using System.ComponentModel;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Tools.ModelContext.Resources;

public static class ModelContextResourceService
{
    [Description("Reads a resource from the specified URI")]
    public static async Task<CallToolResponse> ReadResource(
        [Description("The URI of the resource to get. Must be a valid URI. Also supports links to authenticated content like SharePoint, Outlook, Simplicate, etc")]
        string uri,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(uri);
        var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor?.HttpContext;

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var content = await downloadService.GetContentAsync(uri, httpContext!, cancellationToken) ?? throw new Exception();

        return content.ToReadResourceResult().ToCallToolResponse();
    }
}

