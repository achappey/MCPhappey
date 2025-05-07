using MCPhappey.Core.Extensions;
using MCPhappey.Core.Models.Protocol;
using Microsoft.Graph.Beta;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Services;

public class UploadService(
    IReadOnlyList<ServerConfig> servers)
{
    public async Task<Resource?> UploadToRoot(IMcpServer mcpServer, 
        IServiceProvider serviceProvider, 
        string filename,
        BinaryData binaryData,
        CancellationToken cancellationToken = default)
    {
        if (mcpServer.ClientCapabilities?.Roots == null) return null;
        var roots = await mcpServer.RequestRootsAsync(new(), cancellationToken);
        var server = servers.FirstOrDefault(a => a.Server.ServerInfo.Name == mcpServer.ServerOptions.ServerInfo?.Name);

        var oneDriveRoots = roots.Roots.Where(a => new Uri(a.Uri).Host.EndsWith(".sharepoint.com"));

        if (oneDriveRoots.Any())
        {
            using var graphClient = await serviceProvider.GetOboGraphClient(mcpServer);

            foreach (var oneDriveRoot in oneDriveRoots)
            {
                var driveItem = await graphClient.GetDriveItem(oneDriveRoot.Uri, cancellationToken);

                if (driveItem?.Folder != null)
                {
                    try
                    {
                        var uploadedItem = await graphClient
                            .Drives[driveItem.ParentReference?.DriveId]   // the correct drive
                            .Items[driveItem.Id]                         // that exact folder
                            .ItemWithPath(filename)                      // only the file name!
                            .Content
                            .PutAsync(binaryData.ToStream());

                        if (uploadedItem != null)
                        {
                            return uploadedItem.ToResource();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        return null;
    }

}
