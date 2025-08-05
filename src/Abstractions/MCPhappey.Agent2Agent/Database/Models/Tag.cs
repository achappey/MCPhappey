namespace MCPhappey.Agent2Agent.Database.Models;

public class Tag
{
    public int Id { get; set; }
    public string Value { get; set; } = default!;
    public ICollection<SkillTag> SkillTags { get; set; } = [];
}