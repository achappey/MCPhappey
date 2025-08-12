
using System.Text.Json.Serialization;

namespace MCPhappey.Agent2Agent.Models;

public class Agent2AgentContext
{
    [JsonPropertyName("contextId")]
    public string ContextId { get; set; } = default!;

    [JsonPropertyName("taskIds")]
    public IEnumerable<string> TaskIds { get; set; } = [];

    [JsonPropertyName("ownerIds")]
    public IEnumerable<string> OwnerIds { get; set; } = [];

    [JsonPropertyName("userIds")]
    public IEnumerable<string> UserIds { get; set; } = [];

    [JsonPropertyName("securityGroupIds")]
    public IEnumerable<string> SecurityGroupIds { get; set; } = [];

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = [];

}
