
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.Agent2Agent.Database.Models;

[Index(nameof(Url), IsUnique = true)]
public class McpServer
{
    public int Id { get; set; }
    public string Url { get; set; } = default!;
    public ICollection<AgentServer> AgentServers { get; set; } = [];
}
