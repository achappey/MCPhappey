using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Core.Models.Protocol;

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

    [JsonPropertyName("auth")]
    public ServerAuth? Auth { get; set; }
}

public class ServerAuth
{
    public EntraIdAuth? EntraId { get; set; }

    public OAuth? OAuth { get; set; }

    public Dictionary<string, OBOClient>? OBO { get; set; }

    [JsonPropertyName("jwksUri")]
    public string? JwksUri
    {
        get
        {
            return EntraId != null ? $"https://login.microsoftonline.com/{EntraId.TenantId}/discovery/v2.0/keys"
                : OAuth?.JwksUri;
        }
    }

    [JsonPropertyName("validIssuers")]
    public string[]? ValidIssuers
    {
        get
        {
            return EntraId != null ?
                [
                    $"https://login.microsoftonline.com/{EntraId.TenantId}/v2.0",
                    $"https://sts.windows.net/{EntraId.TenantId}/"
                ]
                : OAuth != null ? [OAuth.Issuer] : null;
        }
    }

    [JsonPropertyName("issuer")]
    public string? Issuer
    {
        get
        {
            return EntraId != null ?
               $"https://login.microsoftonline.com/{EntraId.TenantId}/v2.0"
                : OAuth?.Issuer;
        }
    }

    [JsonPropertyName("authorizationEndpoint")]
    public string? AuthorizationEndpoint
    {
        get
        {
            return EntraId != null ?
               $"https://login.microsoftonline.com/{EntraId.TenantId}/oauth2/v2.0/authorize"
                : OAuth?.AuthorizationEndpoint;
        }
    }

    [JsonPropertyName("tokenEndpoint")]
    public string? TokenEndpoint
    {
        get
        {
            return EntraId != null ?
               $"https://login.microsoftonline.com/{EntraId.TenantId}/oauth2/v2.0/token"
                : OAuth?.TokenEndpoint;
        }
    }

}

public class OAuth
{
    // OAuth config for .well-known and token validation
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = null!; // e.g. https://login.microsoftonline.com/{tenantId}/v2.0

    [JsonPropertyName("audience")]
    public string Audience { get; set; } = null!; // e.g. api://mcp-server-app-id

    [JsonPropertyName("jwksUri")]
    public string JwksUri { get; set; } = null!; // e.g. https://.../discovery/v2.0/keys

    [JsonPropertyName("authorizationEndpoint")]
    public string AuthorizationEndpoint { get; set; } = null!;

    [JsonPropertyName("tokenEndpoint")]
    public string TokenEndpoint { get; set; } = null!;

    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; } = [];

    [JsonPropertyName("requiredRoles")]
    public string[]? RequiredRoles { get; set; }
}


public class EntraIdAuth
{
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = null!;
}

public class OBOClient
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = null!;

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = null!;

    [JsonPropertyName("tokenEndpoint")]
    public string TokenEndpoint { get; set; } = null!;

    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; } = [];
}

public class MCPServerList
{
    [JsonPropertyName("mcpServers")]
    public Dictionary<string, MCPServer> McpServers { get; set; } = [];
}

public class MCPServer
{

    [JsonPropertyName("type")]
    public string Type { get; set; } = "sse";

    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;

    [JsonPropertyName("headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Headers { get; set; }
}


public class Server
{

    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2025-03-26";

    [JsonPropertyName("capabilities")]
    public ServerCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("serverInfo")]
    public ServerInfo ServerInfo { get; set; } = null!;

    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; set; }
}

public class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;

}