using System.Text.Json.Serialization;

namespace MCPhappey.Common.Models;

public class RegistryServer
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://static.modelcontextprotocol.io/schemas/2025-09-16/server.schema.json";

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;

    [JsonPropertyName("remotes")]
    public IEnumerable<ServerRemote>? Remotes { get; set; } = null!;

}

public class ServerRemote
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "streamable-http";

    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
}