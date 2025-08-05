namespace MCPhappey.Agent2Agent.Database.Models;

public class Skill
{
    public int Id { get; set; }

    public int AgentCardId { get; set; }

    public AgentCard AgentCard { get; set; } = null!;

    public string Identifier { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<SkillTag> SkillTags { get; set; } = [];
    
    public ICollection<SkillExample> Examples { get; set; } = [];
}
