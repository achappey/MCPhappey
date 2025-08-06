using MCPhappey.Agent2Agent.Models;

namespace MCPhappey.Agent2Agent.Extensions;

public static class AgentExtensions
{
    public static AgentConfig ToAgentConfig(
        this Database.Models.Agent agent) => new()
        {
            AgentCard = new A2A.Models.AgentCard()
            {
                Name = agent.AgentCard.Name,
                Description = agent.AgentCard.Description,
                Url = new Uri(agent.AgentCard.Url, UriKind.Relative),
                Skills = [.. agent.AgentCard.Skills.Select(t => t.ToSkill())]
            },
            Agent = new Agent()
            {
                Model = agent.Model,
                Temperature = agent.Temperature,
                Owners = [.. agent.Owners.Select(r => r.Id)],
                McpServers = [.. agent.Servers.Select(r => r.McpServer.Url)]
            },
            Auth = agent.AppRegistration != null ? new AgentAuth()
            {
                ClientId = agent.AppRegistration.ClientId,
                Audience = agent.AppRegistration.Audience,
                Scope = agent.AppRegistration.Scope
            } : null
        };

    public static A2A.Models.AgentSkill ToSkill(
        this Database.Models.Skill skill) => new()
        {
            Name = skill.Name,
            Description = skill.Description,
            Id = skill.Identifier,
            Tags = [.. skill.SkillTags.Select(a => a.Tag.Value).ToList()],
            Examples = [.. skill.Examples.Select(a => a.Example).ToList()]
        };

}
