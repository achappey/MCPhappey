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
    [McpServerTool(Destructive = false, OpenWorld = false)]
    [Description("Create a new tag for an Agent2Agent agent skill.")]
    public static async Task<CallToolResult> Agent2AgentEditor_CreateTag(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            string agentName,
            string skillId,
            string tagName,
            CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<AgentRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new NewA2ASkillTag()
        {
            Name = tagName,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();
        var currentAgent = await serverRepository.GetAgent(agentName, cancellationToken);
        if (currentAgent?.Owners.Any(e => e.Id == userId) != true) return "Access denied".ToErrorCallToolResponse();
        var skill = currentAgent.AgentCard.Skills.FirstOrDefault(a => a.Identifier == skillId);
        if (skill == null) return $"Skill with id {skillId} not found".ToErrorCallToolResponse();

        skill.SkillTags.Add(new SkillTag()
        {
            Tag = new Tag()
            {
                Value = typedResult.Name
            }
        });

        var server = await serverRepository.UpdateSkill(skill);

        return JsonSerializer.Serialize(new
        {
            typedResult.Name
        })
        .ToJsonCallToolResponse($"a2a-editor://agent/{currentAgent.AgentCard.Name}");
    }

    [Description("Please fill in the tag details.")]
    public class NewA2ASkillTag
    {

        [JsonPropertyName("name")]
        [Required]
        [Description("A name for the tag.")]
        public string Name { get; set; } = default!;
    }

}

