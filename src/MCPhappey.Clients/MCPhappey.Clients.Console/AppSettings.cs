namespace MCPhappey.Console;

public class AppSettings
{
    public string MCPServer { get; set; } = null!;
    public string? OpenAI_ApiKey { get; set; }
    public bool? ExtendedTest { get; set; }
    public IEnumerable<string>? Servers { get; set; }
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>>? ToolCalls { get; set; }
}
