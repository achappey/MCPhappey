namespace MCPhappey.Agent2Agent.Database.Models;

public class Extension
{
    public int Id { get; set; }

    public int AgentCardId { get; set; }

    public AgentCard AgentCard { get; set; } = null!;

    public string Uri { get; set; } = null!;

    public bool Required { get; set; }

    public string? Description { get; set; }

    public string? Params { get; set; }
}
