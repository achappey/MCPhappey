using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Core.Models.Protocol;

public class PromptTemplates
{
    [JsonPropertyName("prompts")]
    public List<PromptTemplate> Prompts { get; set; } = [];
}

public class PromptTemplate
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = null!;

    [JsonPropertyName("template")]
    public Prompt Template { get; set; } = null!;

    [JsonPropertyName("resourceTemplates")]
    public IEnumerable<string>? ResourceTemplates { get; set; }

    [JsonPropertyName("resources")]
    public IEnumerable<string>? Resources { get; set; }
}
