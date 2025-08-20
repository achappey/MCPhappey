using MCPhappey.Common;
using MCPhappey.Common.Models;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.DependencyInjection;
using MCPhappey.Agent2Agent.Repositories;
using MCPhappey.Auth.Extensions;
using Microsoft.AspNetCore.Http;

namespace MCPhappey.Servers.SQL.Providers;

public class Agent2AgentCompletion : IAutoCompletion
{
    public bool SupportsHost(ServerConfig serverConfig)
        => serverConfig.Server.ServerInfo.Name.StartsWith("Agent2Agent");

    public async Task<Completion> GetCompletion(
        IMcpServer mcpServer,
        IServiceProvider serviceProvider,
        CompleteRequestParams? completeRequestParams,
        CancellationToken cancellationToken = default)
    {
        if (completeRequestParams?.Argument?.Name is not string argName || completeRequestParams.Argument.Value is not string argValue)
            return new();

        IEnumerable<string> values = [];
        switch (completeRequestParams?.Argument?.Name)
        {
            case "context":
                var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
                var oid = serviceProvider.GetUserId();
                var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();
                var context = await contextRepo.GetContextsForUserAsync(oid!, userGroupIds ?? [], cancellationToken);

                values = context.Select(z => z.Metadata.TryGetValue("name", out object? value) ? value.ToString()! : z.ContextId);
                break;
            default:
                break;
        }

        var filtered = values.Where(a => string.IsNullOrEmpty(argValue)
                                    || a.Contains(argValue, StringComparison.OrdinalIgnoreCase));

        return new Completion()
        {
            Values = [..filtered
                            .Order()
                            .Take(100)],
            HasMore = filtered.Count() > 100,
            Total = filtered.Count()
        };
    }

}
