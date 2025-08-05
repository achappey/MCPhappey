namespace MCPhappey.Agent2Agent.Database.Models;

public class AgentServer
{
    public int AgentId { get; set; }
    public Agent Agent { get; set; } = null!;
    public int McpServerId { get; set; }
    public McpServer McpServer { get; set; } = null!;
}
