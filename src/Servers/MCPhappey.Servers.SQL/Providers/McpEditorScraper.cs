using MCPhappey.Common;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Providers;

public class McpEditorScraper : IContentScraper
{
    public bool SupportsHost(ServerConfig serverConfig, string url)
        => new Uri(url).Scheme.Equals("mcp-editor", StringComparison.OrdinalIgnoreCase);

    public async Task<IEnumerable<FileItem>?> GetContentAsync(IMcpServer mcpServer,
        IServiceProvider serviceProvider, string url, CancellationToken cancellationToken = default)
    {
        var tokenService = serviceProvider.GetService<HeaderProvider>();
        if (string.IsNullOrEmpty(tokenService?.Bearer))
        {
            return null;
        }

        if (url.Equals("mcp-editor://statistics"))
        {
            var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
            var servers = await serverRepository.GetServers(cancellationToken);
            //servers.First().Owners.Select(a => a.)
            var totalServers = servers.Count;
            var totalPrompts = servers.Sum(a => a.Prompts?.Count ?? 0);
            var totalResources = servers.Sum(a => a.Resources?.Count ?? 0);
            var totalResourceTemplates = servers.Sum(a => a.ResourceTemplates?.Count ?? 0);

            var allOwnerIds = servers.SelectMany(s => s.Owners)
                             .Select(u => u.Id)
                             .Distinct()
                             .ToList();

            var totalUniqueOwners = allOwnerIds.Count;
            var serversPerOwner = servers
                    .SelectMany(s => s.Owners.Select(u => new { ServerId = s.Id, OwnerId = u.Id }))
                    .GroupBy(x => x.OwnerId)
                    .Select(g => g.Count())
                    .ToList();

            var avgServersPerOwner = totalUniqueOwners == 0 ? 0 : (double)totalServers / totalUniqueOwners;

            // (optional nerd stats)
            var minServersPerOwner = serversPerOwner.Count == 0 ? 0 : serversPerOwner.Min();
            var maxServersPerOwner = serversPerOwner.Count == 0 ? 0 : serversPerOwner.Max();

            // Avoid division by zero, obviously
            var avgPromptsPerServer = totalServers == 0 ? 0 : (double)totalPrompts / totalServers;
            var avgResourcesPerServer = totalServers == 0 ? 0 : (double)totalResources / totalServers;
            var avgTemplatesPerServer = totalServers == 0 ? 0 : (double)totalResourceTemplates / totalServers;

            var stats = new
            {
                TotalServers = totalServers,
                TotalPrompts = totalPrompts,
                TotalResources = totalResources,
                TotalResourceTemplates = totalResourceTemplates,

                AveragePromptsPerServer = avgPromptsPerServer,
                AverageResourcesPerServer = avgResourcesPerServer,
                AverageResourceTemplatesPerServer = avgTemplatesPerServer,

                TotalUniqueOwners = totalUniqueOwners,
                AverageServersPerOwner = avgServersPerOwner,
                MinServersPerOwner = minServersPerOwner,
                MaxServersPerOwner = maxServersPerOwner
            };

            return await Task.FromResult<IEnumerable<FileItem>>([stats.ToFileItem(url)]);
        }

        if (url.Equals("mcp-editor://servers"))
        {
            var servers = await serviceProvider.GetServers(cancellationToken);
            var userServers = servers.Select(z => z.ToMcpServer());

            return [userServers.ToFileItem(url)];
        }

        string serverName = GetServerNameFromEditorUrl(url);
        var server = await serviceProvider.GetServer(serverName, cancellationToken);

        if (url.EndsWith("/prompts", StringComparison.OrdinalIgnoreCase))
        {
            return [.. server.Prompts.Select(z => z.ToPromptTemplate())
                .Select(p => p.ToFileItem(url))];
        }

        if (url.EndsWith("/resources", StringComparison.OrdinalIgnoreCase))
        {
            return [.. server.Resources.Select(z => z.ToResource())
                .Select(r => r.ToFileItem(url))];
        }

        if (url.EndsWith("/resourceTemplates", StringComparison.OrdinalIgnoreCase))
        {
            return [.. server.ResourceTemplates.Select(z => z.ToResourceTemplate())
                .Select(r => r.ToFileItem(url))];
        }

        throw new Exception("Uri not supported");
    }

    private static string GetServerNameFromEditorUrl(string url)
    {
        var uri = new Uri(url);
        // Segments: ["/", "server/", "{serverName}/", ...]
        return uri.Segments.Length >= 3 ? uri.Segments[1].TrimEnd('/') : "";
    }
}
