
using System.Text.Json.Serialization;

namespace MCPhappey.Agent2Agent.Models;

public class Agent2AgentViewContext
{
    [JsonPropertyName("contextId")]
    public string ContextId { get; set; } = default!;

    [JsonPropertyName("taskIds")]
    public IEnumerable<string> TaskIds { get; set; } = [];

    [JsonPropertyName("owners")]
    public IEnumerable<Agent2AgentUser> Owners { get; set; } = [];

    [JsonPropertyName("users")]
    public IEnumerable<Agent2AgentUser> Users { get; set; } = [];

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = [];

}

public class Agent2AgentUser
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;


}