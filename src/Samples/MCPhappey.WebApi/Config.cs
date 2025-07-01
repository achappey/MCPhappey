using MCPhappey.Auth.Models;
using MCPhappey.Simplicate.Options;

namespace MCPhappey.WebApi;

public class Config
{
    public string? McpDatabase { get; set; }

    public string? PrivateKey { get; set; }

    public SimplicateOptions? Simplicate { get; set; }

    public OAuthSettings? OAuth { get; set; }

    public string? KernelMemoryDatabase { get; set; }

    public Agent2AgentStorage? Agent2AgentStorage { get; set; }

    public Dictionary<string, Dictionary<string, string>>? Domains { get; set; }

}

public class Agent2AgentStorage
{
    public string ConnectionString { get; set; } = default!;
    public string TaskContainer { get; set; } = default!;
    public string ContextContainer { get; set; } = default!;
}