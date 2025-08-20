
namespace MCPhappey.Agent2Agent.Database.Models;

public class Agent
{
    public int Id { get; set; }

    public string Model { get; set; } = null!;

    public float Temperature { get; set; }

    public int AgentCardId { get; set; }

    public AgentCard AgentCard { get; set; } = default!;
  //  public AnthropicMetadata? Anthropic { get; set; }

  //  public OpenAIMetadata? OpenAI { get; set; }

    public AppRegistration? AppRegistration { get; set; }

    public ICollection<AgentOwner> Owners { get; set; } = [];

  //  public ICollection<AgentServer> Servers { get; set; } = [];



}
