using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.Agent2Agent.Database.Models;

[Owned]
public class AnthropicMetadata
{
    [JsonPropertyName("thinking")]
    public AnthropicThinking? Thinking { get; set; }

    // "code_execution": undefined  -> leave null when not used
    [JsonPropertyName("code_execution")]
    public AnthropicCodeExecution? CodeExecution { get; set; }

    [JsonPropertyName("web_search")]
    public AnthropicWebSearch? WebSearch { get; set; }
}

[Owned]
public class AnthropicThinking
{
    [JsonPropertyName("budget_tokens")]
    public int? BudgetTokens { get; set; }
}

// Keep this flexible. If you later need structure (e.g., enabled, container),
// add properties here without breaking callers that send/expect null.
[Owned]
public class AnthropicCodeExecution
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "code_execution_20250522";

    // Example optional toggle if you ever need it:
    // [JsonPropertyName("enabled")]
    // public bool? Enabled { get; set; }
}

[Owned]
public class AnthropicWebSearch
{
    [JsonPropertyName("max_uses")]
    public int? MaxUses { get; set; }

    [JsonPropertyName("allowed_domains")]
    public IEnumerable<string>? AllowedDomains { get; set; }

    [JsonPropertyName("blocked_domains")]
    public IEnumerable<string>? BlockedDomains { get; set; }
}
