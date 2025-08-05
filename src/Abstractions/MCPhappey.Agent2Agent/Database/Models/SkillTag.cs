namespace MCPhappey.Agent2Agent.Database.Models;

public class SkillTag
{
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
