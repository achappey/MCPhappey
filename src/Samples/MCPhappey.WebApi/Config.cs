using MCPhappey.Auth.Models;
using MCPhappey.Common.Models;
using MCPhappey.Simplicate.Options;
using MCPhappey.Tools.AI;

namespace MCPhappey.WebApi;

public class Config
{
    public string? McpDatabase { get; set; }

    public string? TelemetryDatabase { get; set; }

    public string? DarkIcon { get; set; }

    public string? LightIcon { get; set; }

    public string? PrivateKey { get; set; }

    public SimplicateOptions? Simplicate { get; set; }

    public OAuthSettings? OAuth { get; set; }

    public McpApplicationInsights? ApplicationInsights { get; set; }

    public string? KernelMemoryDatabase { get; set; }

    public Agent2AgentStorage? Agent2AgentStorage { get; set; }

    public Dictionary<string, Dictionary<string, string>>? DomainHeaders { get; set; }

    public Dictionary<string, McpExtension>? McpExtensions { get; set; }

    public Dictionary<string, Dictionary<string, string>>? DomainQueryStrings { get; set; }

}

public class Agent2AgentStorage
{
    public string ConnectionString { get; set; } = default!;
    public string TaskContainer { get; set; } = default!;
    public string ContextContainer { get; set; } = default!;
    public string Database { get; set; } = default!;
}
