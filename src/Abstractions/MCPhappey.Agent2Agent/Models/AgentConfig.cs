using System.Text.Json.Serialization;
using A2A.Models;

namespace MCPhappey.Agent2Agent.Models;

public class Agent
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = default!;

    [JsonPropertyName("toolChoice")]
    public string? ToolChoice { get; set; }

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }

    [JsonPropertyName("historyRetention")]
    public int HistoryRetention { get; set; }

    [JsonPropertyName("mcpServers")]
    public List<string> McpServers { get; set; } = [];

    [JsonPropertyName("owners")]
    public List<string> Owners { get; set; } = [];

}

// This is the full wrapper object, with agentCard as an external type
public class AgentConfig
{
    [JsonPropertyName("agentCard")]
    public AgentCard AgentCard { get; set; } = default!;

    [JsonPropertyName("agent")]
    public Agent Agent { get; set; } = default!;

    [JsonPropertyName("auth")]
    public AgentAuth? Auth { get; set; }
}

public class AgentAuth
{
    public string? ClientId { get; set; }
    public string? Audience { get; set; }
    public string? Scope { get; set; }
}