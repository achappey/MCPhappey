using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.AI;

public static class ChatApp
{
    [Description("Generate a conversation name from initial user and assistant messages")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<ContentBlock> ChatApp_ExecuteGenerateConversationName(
        [Description("User message")] string userMessage,
        [Description("Assistant message")] string assistantMessage,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();

        // Arguments for the prompt template
        var promptArgs = new Dictionary<string, JsonElement>
        {
            ["userMessage"] = JsonSerializer.SerializeToElement(userMessage ?? string.Empty),
            ["assistantMessage"] = JsonSerializer.SerializeToElement(assistantMessage ?? string.Empty)
        };

        // Pick the model you want (same as before or allow config)
        var modelName = "gpt-4.1-mini"; // or any default model you prefer

        // Optional: Logging/notification
        var markdown = $"Generating conversation name...\nUser: {userMessage}\nAssistant: {assistantMessage}";
        await mcpServer.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);

        // Call prompt template (should be named "conversation-name")
        var result = await samplingService.GetPromptSample(
            serviceProvider,
            mcpServer,
            "conversation-name", // prompt template name
            promptArgs,
            modelName,
            cancellationToken: cancellationToken
        );

        // Return the result as a single ContentBlock
        return result.Content;
    }

    [Description("Generate a very short, friendly welcome message for a chatbot interface")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<ContentBlock> ChatApp_ExecuteGenerateWelcomeMessage(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();

        // Pick the model you want
        var modelName = "gpt-4.1-nano"; // or set to your preferred model

        // Optional: Logging/notification
        var markdown = $"Generating welcome message";
        await mcpServer.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);

        // Call prompt template (should be named "welcome-message")
        var result = await samplingService.GetPromptSample(
            serviceProvider,
            mcpServer,
            "welcome-message", // prompt template name
            modelHint: modelName,
            cancellationToken: cancellationToken
        );

        // Return the result as a single ContentBlock
        return result.Content;
    }
}

