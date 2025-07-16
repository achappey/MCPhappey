using MCPhappey.Common;
using MCPhappey.Common.Models;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using MCPhappey.Servers.SQL.Repositories;
using MCPhappey.Auth.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MCPhappey.Servers.SQL.Providers;

public class EditorCompletion : IAutoCompletion
{
    public bool SupportsHost(ServerConfig serverConfig)
        => serverConfig.Server.ServerInfo.Name.StartsWith("ModelContext-Editor");

    public async Task<CompleteResult?> GetCompletion(
     IMcpServer mcpServer,
     IServiceProvider serviceProvider,
     CompleteRequestParams? completeRequestParams,
     CancellationToken cancellationToken = default)
    {
        if (completeRequestParams?.Argument?.Name is not string argName || completeRequestParams.Argument.Value is not string argValue)
            return new CompleteResult();

        IServerDataProvider sqlServerDataProvider = serviceProvider.GetRequiredService<IServerDataProvider>();
        ServerRepository serverRepository = serviceProvider.GetRequiredService<ServerRepository>();

        var userId = serviceProvider.GetUserId();

        IEnumerable<string> values = [];
        switch (completeRequestParams?.Argument?.Name)
        {
            case "server":
                var servers = await serverRepository.GetServers(cancellationToken);
                values = servers.Where(a => a.Owners.Any(a => a.Id == userId))
                    .Select(z => z.Name);

                break;
            case "resourceName":
            case "promptName":
                if (completeRequestParams.Context?.Arguments?.ContainsKey("server") == true)
                {
                    var serverName = completeRequestParams.Context?.Arguments["server"];

                    if (!string.IsNullOrEmpty(serverName))
                    {
                        switch (completeRequestParams?.Argument?.Name)
                        {
                            case "resourceName":
                                var resources = await sqlServerDataProvider.GetResourcesAsync(serverName, cancellationToken);
                                values = resources.Resources.Select(z => z.Name);
                                break;
                            case "promptName":
                                var prompts = await sqlServerDataProvider.GetPromptsAsync(serverName, cancellationToken);
                                values = prompts.Select(z => z.Template.Name);
                                ;
                                break;
                            default:
                                break;
                        }
                    }
                }

                break;

            default:
                break;
        }

        return new CompleteResult
        {
            Completion = new()
            {
                Values = [.. values.Where(a => string.IsNullOrEmpty(argValue) || a.Contains(argValue, StringComparison.OrdinalIgnoreCase))]
            }
        };

    }

}
