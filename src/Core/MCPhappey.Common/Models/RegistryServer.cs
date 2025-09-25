using System.Text.Json.Serialization;

namespace MCPhappey.Common.Models;

public class RegistryServer
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://static.modelcontextprotocol.io/schemas/2025-09-16/server.schema.json";

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("websiteUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;

    [JsonPropertyName("remotes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ServerRemote>? Remotes { get; set; }

    [JsonPropertyName("repository")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Repository? Repository { get; set; }

}

public class Repository
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = null!;

    [JsonPropertyName("subfolder")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subfolder { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
}

public class ServerRemote
{
    [JsonPropertyName("headers")]
    public IEnumerable<ServerHeader>? Headers { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "streamable-http";

    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
}

public class ServerHeader
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("isSecret")]
    public bool? IsSecret { get; set; }

    [JsonPropertyName("isRequired")]
    public bool? IsRequired { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }
}