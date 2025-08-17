using Microsoft.EntityFrameworkCore;

namespace MCPhappey.Agent2Agent.Database.Models;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Url), IsUnique = true)]
public class AgentCard
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; } = null!;

    public string Url { get; set; } = null!;

    public ICollection<Skill> Skills { get; set; } = [];

    public ICollection<Extension> Extensions { get; set; } = [];

    public int AgentId { get; set; }

    public Agent Agent { get; set; } = default!;
}
