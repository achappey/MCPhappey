using System.ComponentModel;
using MCPhappey.Common.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Agent;

public static class AgentService
{
    [Description("Use the tool to think about something. It will not obtain new information or change the database, but just append the thought to the log. Use it when complex reasoning or some cache memory is needed.")]
    [McpServerTool(Title = "Think and append to log", ReadOnly = true, OpenWorld = false)]
    public static async Task<CallToolResult> Agent_Think(
        [Description("A thought to think about.")] string thought) =>
            await Task.FromResult(thought.ToTextCallToolResponse());

}

