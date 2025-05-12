namespace MCPhappey.WebApi;

public class Config
{
    public string? Agent2AgentDiscovery { get; set; }

    public string? McpDatabase { get; set; }
    
    public string? PrivateKey { get; set; }

    public string? KernelMemoryDatabase { get; set; }

    public Dictionary<string, Dictionary<string, string>>? Domains { get; set; }

}