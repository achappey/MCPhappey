using System.ComponentModel;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.OneDrive;

public static class GraphOneDrive
{
    [Description("Uploads a file to the specified OneDrive location.")]
    [McpServerTool(Name = "GraphOneDrive_UploadFile", ReadOnly = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphOneDrive_UploadFile(
        [Description("The OneDrive Drive ID.")] string driveId,
        [Description("The file name (e.g. foo.txt).")] string filename,
        [Description("The folder path in OneDrive (e.g. docs).")] string path,
        [Description("The file contents as a string.")] string content,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var result = await client.Drives[driveId]
                .Items["root"].ItemWithPath($"/{path}/{filename}")
                .Content.PutAsync(BinaryData.FromString(content).ToStream(),
                   cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/drives/{driveId}/items/root:/{path}/{filename}:/content");
    }


}