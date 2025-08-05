using System.ComponentModel;
using System.Text.Json.Serialization;
using MCPhappey.Agent2Agent.Repositories;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Agent2Agent;

public static class Agent2AgentOrchestrator
{
 /*   [McpServerTool(Name = "Agent2AgentOrchestrator_GetContexts", ReadOnly = true, UseStructuredContent = true)]
    [Description("Returns all agent contexts accessible to the current user, based on their user ID or security group memberships. Contexts contain metadata and task references for agent orchestration.")]
    public static async Task<IEnumerable<Agent2AgentContext>> Agent2AgentOrchestrator_GetContexts(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var oid = tokenProvider.GetOidClaim();
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);

        var repo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
        // Option 1: all contexts (filter by user as you wish)
        var contexts = await repo.GetContextsForUserAsync(oid, cancellationToken);
        return contexts;
    }

    [McpServerTool(Name = "Agent2AgentOrchestrator_CreateContext", ReadOnly = true, UseStructuredContent = true)]
    [Description("Create a new A2A context")]
    public static async Task<Agent2AgentContext> Agent2AgentOrchestrator_CreateContext(
       [Description("Name of the new context")] string name,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var oid = tokenProvider.GetOidClaim();
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);

        var repo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();

        var newContext = new Agent2AgentContext()
        {
            ContextId = Guid.NewGuid().ToString(),
            Name = name,
            OwnerIds = [oid],
            UserIds = [oid]
        };

        await repo.SaveContextAsync(newContext, cancellationToken);
        return newContext;
    }

    public class Agent2AgentContext
    {
        [JsonPropertyName("contextId")]
        public string ContextId { get; set; } = default!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("taskIds")]
        public IEnumerable<string> TaskIds { get; set; } = [];

        [JsonPropertyName("ownerIds")]
        public IEnumerable<string> OwnerIds { get; set; } = [];

        [JsonPropertyName("userIds")]
        public IEnumerable<string> UserIds { get; set; } = [];

        [JsonPropertyName("securityGroupIds")]
        public IEnumerable<string> SecurityGroupIds { get; set; } = [];

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = [];

    }*/
}

