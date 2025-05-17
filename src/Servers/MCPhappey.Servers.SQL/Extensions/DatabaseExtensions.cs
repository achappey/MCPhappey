using MCPhappey.Common.Constants;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Servers.SQL.Extensions;

public static class DatabaseExtensions
{
    public static Server ToMcpServer(this Models.Server server)
        => new()
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
            Groups = server.Secured && server.Groups.Count > 0
                ? server.Groups.Select(a => a.Id) : null,
            Owners = server.Owners.Count > 0 ? server.Owners.Select(a => a.Id) : null,
            OBO = server.Secured ? new Dictionary<string, string>() {
                { Hosts.MicrosoftGraph, "https://graph.microsoft.com/User.Read"} } : null,
        };

    public static ListResourcesResult ToListResourcesResult(this ICollection<Models.Resource> resources)
        => new()
        {
            Resources = [.. resources.Select(a => new Resource()
            {
                Uri = a.Uri,
                Name = a.Name,
                Description = a.Description
            })]
        };
}