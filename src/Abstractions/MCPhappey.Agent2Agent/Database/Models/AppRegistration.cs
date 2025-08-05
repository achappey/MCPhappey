
namespace MCPhappey.Agent2Agent.Database.Models;

public class AppRegistration
{
    public int Id { get; set; }

    public int AgentId { get; set; }

    public Agent Agent { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public string Scope { get; set; } = null!;



}
