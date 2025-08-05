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
    [McpServerTool(Name = "Agent2AgentEditor_SetAppRegistration")]
    [Description("Sets the app registrations for an Agent2Agent agent.")]
    public static async Task<CallToolResult> Agent2AgentEditor_SetAppRegistration(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            string agentName,
            string clientId,
            string clientSecret,
            string audience,
            string scope,
            CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<AgentRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var (typedResult, notAccepted) = await requestContext.Server.TryElicit(new NewA2AAgentAppRegistration()
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Audience = audience,
            Scope = scope,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();
        var currentAgent = await serverRepository.GetAgent(agentName, cancellationToken);
        if (currentAgent?.Owners.Any(e => e.Id == userId) != true) return "Access denied".ToErrorCallToolResponse();

        var server = await serverRepository.CreateAppRegistration(new AppRegistration()
        {
            ClientId = typedResult.ClientId,
            Audience = typedResult.Audience,
            AgentId = currentAgent.Id,
            ClientSecret = typedResult.ClientSecret,
            Scope = typedResult.Scope,
        }, cancellationToken);

        // currentAgent.AppRegistration = server;
        //await serverRepository.UpdateAgent(currentAgent!);
        return JsonSerializer.Serialize(new
        {
            server.ClientId,
            server.Audience,
            server.Scope,
        })
        .ToJsonCallToolResponse($"a2a-editor://agent/{agentName}");
    }

    [Description("Please fill in the Agent app registration details.")]
    public class NewA2AAgentAppRegistration
    {
        [JsonPropertyName("clientId")]
        [Required]
        [Description("The Application (client) ID issued by Entra ID.")]
        public string ClientId { get; set; } = default!;

        [JsonPropertyName("clientSecret")]
        [Required]
        [Description("The client secret (or certificate thumbprint) associated with the app registration.")]
        public string ClientSecret { get; set; } = default!;

        [JsonPropertyName("audience")]
        [Required]
        [Description("The intended audience (resource URI) for access tokens issued to this app.")]
        public string Audience { get; set; } = default!;

        [JsonPropertyName("scope")]
        [Required]
        [Description("The delegated or application scope requested by the agent when calling other services.")]
        public string Scope { get; set; } = default!;
    }

}

