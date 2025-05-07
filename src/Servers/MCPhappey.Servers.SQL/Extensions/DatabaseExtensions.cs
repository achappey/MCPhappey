using MCPhappey.Core.Constants;
using MCPhappey.Core.Models.Protocol;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Servers.SQL.Extensions;

public static class DatabaseExtensions
{
    public static Server ToMcpServer(this Models.Server server)
    {
        return new Server()
        {
            Capabilities = new()
            {
                Prompts = server.Prompts.Count > 0 ? new() : null,
                Resources = server.Resources.Count > 0 ? new() : null,
                Tools = server.Tools.Count > 0 ? new() : null
            },
            Instructions = server.Instructions,
            ServerInfo = new()
            {
                Name = server.Name,
                Version = "1.0.0"
            },
            Metadata = new Dictionary<string, string>()
            {
                {ServerMetadata.Owners, string.Join(",", server.Owners.Select(a => a.Id))},
                {ServerMetadata.McpSource, Enum.GetName(ServerMetadata.McpSources.Database)!}
            }
        };
    }

    public static ListResourcesResult ToListResourcesResult(this ICollection<Models.Resource> resources)
    {
        return new()
        {
            Resources = [.. resources.Select(a => new Resource()
            {
                Uri = a.Uri,
                Name = a.Name,
                Description = a.Description
            })]

        };
    }
}