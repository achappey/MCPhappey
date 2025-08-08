using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Agent2Agent.Database.Models;
using MCPhappey.Agent2Agent.Repositories;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Agent2Agent;

public static partial class Agent2AgentEditor
{
    [McpServerTool(Name = "Agent2AgentEditor_CreateAgent")]
    [Description("Create a new Agent2Agent agent.")]
    public static async Task<CallToolResult> Agent2AgentEditor_CreateAgent(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            string newAgentName,
            string newAgentUrl,
            string agentModel = "gpt-5-mini",
            float temperature = 0,
            string? description = null,
            CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<AgentRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var agentExists = await serverRepository.AgentExists(newAgentName, newAgentUrl, cancellationToken);
        if (agentExists == true) return "Servername already in use".ToErrorCallToolResponse();

        var (typedResult, notAccepted) = await requestContext.Server.TryElicit(new NewA2AAgent()
        {
            Name = newAgentName,
            Temperature = temperature,
            Description = description,
            Url = newAgentUrl,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();

        var server = await serverRepository.CreateAgent(new Agent()
        {
            Model = agentModel,
            Temperature = typedResult.Temperature ?? 0,
            AgentCard = new AgentCard()
            {
                Name = typedResult.Name,
                Description = typedResult.Description,
                Url = typedResult.Url
            },
            Owners = [new AgentOwner() {
                Id = userId
            }],

        }, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            server.Model,
            server.Temperature,
            AgentCard = new
            {
                server.AgentCard.Name,
                server.AgentCard.Url,
                server.AgentCard.Description
            },
            Owners = new List<string>() { userId }
        })
        .ToJsonCallToolResponse($"a2a-editor://agent/{server.AgentCard.Name}");
    }

    [McpServerTool(Name = "Agent2AgentEditor_AddMcpServer")]
    [Description("Add a MCP server to an Agent2Agent agent.")]
    public static async Task<CallToolResult> Agent2AgentEditor_AddMcpServer(
               IServiceProvider serviceProvider,
               RequestContext<CallToolRequestParams> requestContext,
               string agentName,
               string mcpServerUrl,
               CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<AgentRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var (typedResult, notAccepted) = await requestContext.Server.TryElicit(new AddA2AAgentMcpServer()
        {
            Url = new Uri(mcpServerUrl),
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();
        var mcpServer = await serverRepository.GetMcpServer(typedResult.Url.ToString(), cancellationToken);
        var currentAgent = await serverRepository.GetAgent(agentName, cancellationToken);
        if (currentAgent?.Owners.Any(e => e.Id == userId) != true) return "Access denied".ToErrorCallToolResponse();

        if (mcpServer == null)
        {
            var newMcp = await serverRepository.CreateMcpServer(new McpServer()
            {
                Url = typedResult.Url.ToString()
            }, cancellationToken);

            currentAgent.Servers.Add(new AgentServer()
            {
                McpServer = newMcp
            });
        }
        else
        {
            currentAgent.Servers.Add(new AgentServer()
            {
                McpServer = mcpServer
            });
        }

        var server = await serverRepository.UpdateAgent(currentAgent!);

        return JsonSerializer.Serialize(new
        {
            server.Model,
            server.Temperature,
            AgentCard = new
            {
                server.AgentCard.Name,
                server.AgentCard.Url,
                server.AgentCard.Description
            },
            Owners = new List<string>() { userId }
        })
        .ToJsonCallToolResponse($"a2a-editor://agent/{server.AgentCard.Name}");
    }


    [Description("Please fill in the MCP details.")]
    public class AddA2AAgentMcpServer
    {
        [JsonPropertyName("url")]
        [Required]
        [Description("The Agent url.")]
        public Uri Url { get; set; } = default!;

    }


    [Description("Please fill in the Agent details.")]
    public class NewA2AAgent
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The Agent name.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("url")]
        [Required]
        [Description("The Agent url.")]
        public string Url { get; set; } = default!;

        [JsonPropertyName("temperature")]
        [Range(0, 1)]
        [Description("The Agent temperature.")]
        public float? Temperature { get; set; }

        [JsonPropertyName("description")]
        [Description("The Agent description.")]
        public string? Description { get; set; }
    }

}

