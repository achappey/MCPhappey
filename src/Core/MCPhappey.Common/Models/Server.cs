using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Common.Models;

public class ServerConfig
{
    [JsonPropertyName("server")]
    public Server Server { get; set; } = null!;

    [JsonPropertyName("prompts")]
    public PromptTemplates? PromptList { get; set; }

    [JsonPropertyName("resources")]
    public ListResourcesResult? ResourceList { get; set; }

    [JsonPropertyName("resourceTemplates")]
    public ListResourceTemplatesResult? ResourceTemplateList { get; set; }

    [JsonPropertyName("tools")]
    public IEnumerable<string>? ToolList { get; set; }

    [JsonPropertyName("sourceType")]
    public ServerSourceType SourceType { get; init; }


}

public class MCPServerList
{
    [JsonPropertyName("servers")]
    public Dictionary<string, MCPServer> Servers { get; set; } = [];
}

public class MCPServerSettingsList
{
    [JsonPropertyName("mcpServers")]
    public Dictionary<string, MCPServerSettings> McpServers { get; set; } = [];
}


public class MCPServer
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "http";

    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;

    [JsonPropertyName("headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Headers { get; set; }
}

public class MCPServerSettings
{
    [JsonPropertyName("transportType")]
    public string TransportType { get; set; } = "http";

    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;

    [JsonPropertyName("headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Headers { get; set; }
}

public class GradioPlugin
{

    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("transport")]
    public MCPServer Transport { get; set; } = null!;
}

public class Server
{
    [JsonPropertyName("capabilities")]
    public ServerCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("serverInfo")]
    public ServerInfo ServerInfo { get; set; } = null!;

    [JsonPropertyName("headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("obo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? OBO { get; set; }

    [JsonPropertyName("plugins")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Plugins { get; set; }

    [JsonPropertyName("roles")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Roles { get; set; }

    [JsonPropertyName("owners")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Owners { get; set; }

    [JsonPropertyName("groups")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Groups { get; set; }

    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }

    [JsonPropertyName("baseMcp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseMcp { get; set; }

    [JsonPropertyName("hidden")]
    public bool? Hidden { get; set; }

    [JsonIgnore]
    public McpExtension? McpExtension { get; set; }
}

public class McpExtension
{
    public string? Url { get; set; }

    // Optional: headers (like Authorization or x-api-key)
    public Dictionary<string, string>? Headers { get; set; }
}


public class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;
}

public enum ServerSourceType { Static, Dynamic }