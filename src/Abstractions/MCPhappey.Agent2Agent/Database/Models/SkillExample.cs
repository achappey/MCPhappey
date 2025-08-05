namespace MCPhappey.Agent2Agent.Database.Models;

public class SkillExample
{
    public int Id { get; set; }

    public int SkillId { get; set; }

    public Skill Skill { get; set; } = null!;

    public string Example { get; set; } = null!;
}
