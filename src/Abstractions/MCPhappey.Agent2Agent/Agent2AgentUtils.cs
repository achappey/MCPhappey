using System.ComponentModel;
using A2A.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Agent2Agent;

public static class Agent2AgentUtils
{
  /*  [McpServerTool(Name = "Agent2AgentUtils_GetArtifact", ReadOnly = true, UseStructuredContent = true)]
    [Description("Retrieves the specified artifact by ID if the current user or their security groups have access to its context.")]
    public static async Task<Artifact?> Agent2AgentUtils_GetArtifact(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        string taskId,
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        var task = await Agent2AgentContextOrchestrator.Agent2AgentContextOrchestrator_GetTask(serviceProvider,
            requestContext, taskId, cancellationToken);

        return task.Artifacts?.FirstOrDefault(a => a.ArtifactId == artifactId);

    }*/

}

