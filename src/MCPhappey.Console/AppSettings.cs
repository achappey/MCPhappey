public class AppSettings
{
    public string MCPServerBackend { get; set; } = null!;
    public string? OpenAI_ApiKey { get; set; }
    public bool? ExtendedTest { get; set; }

    public string? AuthHost { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public IEnumerable<string>? Servers { get; set; }

    public Dictionary<string, Dictionary<string, Dictionary<string, string>>>? ToolCalls { get; set; }
}
