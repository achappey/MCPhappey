using System.Text.Json.Serialization;

namespace MCPhappey.Agent2Agent.Models;

public record AgentExtension
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = default!;

    [JsonPropertyName("params")]
    public virtual object? Params { get; set; }
}