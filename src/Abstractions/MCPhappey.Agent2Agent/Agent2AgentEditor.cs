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
    [McpServerTool(Destructive = false)]
    [Description("Create a new Agent2Agent agent.")]
    public static async Task<CallToolResult> Agent2AgentEditor_CreateAgent(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            string newAgentName,
            string newAgentUrl,
            string agentModel = "gpt-5-mini",
            float temperature = 1,
            string? description = null,
            CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<AgentRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var agentExists = await serverRepository.AgentExists(newAgentName, newAgentUrl, cancellationToken);
        if (agentExists == true) return "Servername already in use".ToErrorCallToolResponse();

        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new NewA2AAgent()
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

    [McpServerTool(Destructive = false)]
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

        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new AddA2AAgentMcpServer()
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

    [McpServerTool()]
    [Description("Set up OpenAI provider specific metadata like code_interpreter, web_search_preview, etc.")]
    public static async Task<CallToolResult> Agent2AgentEditor_SetOpenAIMetadata(
              IServiceProvider serviceProvider,
              RequestContext<CallToolRequestParams> requestContext,
              string agentName,
              CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<AgentRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();
        var currentAgent = await serverRepository.GetAgent(agentName, cancellationToken);

        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new A2AAgentOpenAISettings()
        {
            ParallelToolCalls = currentAgent?.OpenAI?.ParallelToolCalls ?? true,
            Reasoning = currentAgent?.OpenAI?.Reasoning != null,
            ReasoningEffort = currentAgent?.OpenAI?.Reasoning?.Effort ?? ReasoningEffort.low,
            ReasoningSummary = currentAgent?.OpenAI?.Reasoning?.Summary ?? ReasoningSummary.auto
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();
        if (currentAgent == null) return "Something went wrong".ToErrorCallToolResponse();

        currentAgent.OpenAI = new OpenAIMetadata()
        {
            ParallelToolCalls = typedResult.ParallelToolCalls,
            Reasoning = typedResult.Reasoning ? new Reasoning()
            {
                Effort = typedResult.ReasoningEffort,
                Summary = typedResult.ReasoningSummary
            } : null,
            CodeInterpreter = typedResult.CodeInterpreter ? new CodeInterpreter()
            {

            } : null,
            FileSearch = !string.IsNullOrEmpty(typedResult.FileSearchVectorStoreIds) ? new FileSearch()
            {
                VectorStoreIds = typedResult.FileSearchVectorStoreIds.Split(",")
            } : null
        };

        var server = await serverRepository.UpdateAgent(currentAgent);

        return JsonSerializer.Serialize(new
        {
            server.Model,
            server.Temperature,
            server.OpenAI,
            AgentCard = new
            {
                server.AgentCard.Name,
                server.AgentCard.Url,
                server.AgentCard.Description
            },
            Owners = new List<string>() { userId }
        }, JsonSerializerOptions.Web)
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


    [Description("Please fill in the OpenAI settings details.")]
    public class A2AAgentOpenAISettings
    {
        [JsonPropertyName("parallel_tool_calls")]
        [Description("Enable or disable parallel tool calls.")]
        public bool ParallelToolCalls { get; set; } = true;

        [JsonPropertyName("code_interpreter")]
        [Description("Enable or disable code interpreter.")]
        public bool CodeInterpreter { get; set; } = true;

        [JsonPropertyName("code_interpreter_container_id")]
        [Description("The container ID for the Code Interpreter (leave empty if not used).")]
        public string? CodeInterpreterContainerId { get; set; }

        [JsonPropertyName("reasoning")]
        [DefaultValue(true)]
        [Description("Enable or disable reasoning.")]
        public bool Reasoning { get; set; } = true;

        [JsonPropertyName("reasoning_effort")]
        [Required]
        [Description("The reasoning effort: minimal, low, medium, high.")]
        public ReasoningEffort ReasoningEffort { get; set; } = ReasoningEffort.low;

        [JsonPropertyName("reasoning_summary")]
        [Required]
        [Description("The reasoning summary style: auto, concise, detailed.")]
        public ReasoningSummary ReasoningSummary { get; set; } = ReasoningSummary.auto;

        [JsonPropertyName("file_search_vector_store_ids")]
        [Description("Comma-separated list of Vector Store IDs for file search.")]
        public string? FileSearchVectorStoreIds { get; set; }
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

