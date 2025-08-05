using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Agent2Agent.Database.Models;
using MCPhappey.Agent2Agent.Repositories;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Agent2Agent;

public static partial class Agent2AgentEditor
{
    [McpServerTool(Name = "Agent2AgentEditor_CreateSkill")]
    [Description("Create a new skill for an Agent2Agent agent.")]
    public static async Task<CallToolResult> Agent2AgentEditor_CreateSkill(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            string agentName,
            string skillId,
            string skillName,
            string? skillDescription = null,
            CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<AgentRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var (typedResult, notAccepted) = await requestContext.Server.TryElicit(new NewA2AAgentSkill()
        {
            Id = skillId.Slugify().ToLowerInvariant(),
            Name = skillName,
            Description = skillDescription,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();
        var currentAgent = await serverRepository.GetAgent(agentName, cancellationToken);
        if (currentAgent?.Owners.Any(e => e.Id == userId) != true) return "Access denied".ToErrorCallToolResponse();

        var server = await serverRepository.CreateSkill(new Skill()
        {
            Name = typedResult.Name,
            AgentCard = currentAgent.AgentCard,
            Description = typedResult.Description,
            Identifier = typedResult.Id,
        }, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            server.Name,
            server.Description,
        })
        .ToJsonCallToolResponse($"a2a-editor://agent/{server.AgentCard.Name}");
    }


    [Description("Please fill in the Agent skill details.")]
    public class NewA2AAgentSkill
    {
        [JsonPropertyName("id")]
        [Required]
        [Description("A unique identifier for the agent's skill.")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("name")]
        [Required]
        [Description("A human-readable name for the skill.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("description")]
        [Description("A detailed description of the skill, intended to help clients or users understand its purpose and functionality.")]
        public string? Description { get; set; }
    }

}

