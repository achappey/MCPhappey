using System.ComponentModel;
using System.Text.Json;
using DocumentFormat.OpenXml.Wordprocessing;
using MCPhappey.Common;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.AI;

public static class ChatApp
{
    [Description("Generate a conversation name from initial user and assistant messages")]
    [McpServerTool(Title = "Generate conversation name",
         ReadOnly = true)]
    public static async Task<ContentBlock> ChatApp_GenerateConversationName(
        [Description("User message")] string userMessage,
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
        };

        // Pick the model you want (same as before or allow config)
        var modelName = "gpt-5-mini"; // or any default model you prefer

        // Optional: Logging/notification
        var markdown = $"Generating conversation name...\nUser: {userMessage}";
        await mcpServer.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);

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

    [Description("Get MCP server usage statistics")]
    [McpServerTool(Title = "Get server usage statistics",
         ReadOnly = true)]
    public static async Task<CallToolResult> ChatApp_GetMcpServerStats(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        // --- Resolve configuration ------------------------------------------------
        var config = serviceProvider.GetService<McpApplicationInsights>();
        var serverList = serviceProvider.GetService<IReadOnlyList<ServerConfig>>();

        if (config == null || string.IsNullOrWhiteSpace(config.AppId) || string.IsNullOrWhiteSpace(config.AppKey))
            return "Stats not configured".ToErrorCallToolResponse();

        var baseline = new[] { "/chatapp", "/token", "/register" };

        var hiddenServers = serverList?
            .Where(x => x.Server.Hidden == true)
            .Select(x => x.Server.ServerInfo.Name.ToLowerInvariant())
            .ToArray() ?? [];

        var hasAnyValues = baseline.Concat(hiddenServers)
                                   // escape any embedded quotes, then wrap in quotes for Kusto
                                   .Select(s => $"\"{s.Replace("\"", "\\\"")}\"");

        var hasAnyList = string.Join(", ", hasAnyValues);

        // ------------------------------------------------------------------------
        // Kusto query – 90 days, per‑URL totals, top 10 000
        // ------------------------------------------------------------------------
        var kql = $@"
            requests
            | where timestamp > ago(14d)
            | where name startswith ""POST""
            | where not(tolower(url) has_any({hasAnyList}))
            | summarize TotalRequests = count() by Url = url
            | order by TotalRequests desc
            | take 10000";

        // --- Build REST call -------------------------------------------------------
        var queryUri =
            $"https://api.applicationinsights.io/v1/apps/{config.AppId}/query?query={Uri.EscapeDataString(kql)}";

        var httpFactory = serviceProvider.GetService<IHttpClientFactory>();
        using var http = httpFactory?.CreateClient(nameof(ChatApp_GetMcpServerStats)) ?? new HttpClient();

        http.DefaultRequestHeaders.Add("x-api-key", config.AppKey);

        // --- Execute and handle response ------------------------------------------
        using var response = await http.GetAsync(queryUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            return $"Application Insights query failed ({(int)response.StatusCode}): {details}"
                .ToErrorCallToolResponse();
        }

        var stream = await response.Content.ReadAsStringAsync(cancellationToken);

        return stream.ToJsonCallToolResponse(queryUri);
    }

    [Description("Get available completions that can be used in prompts and can be completed during using those prompts.")]
    [McpServerTool(Title = "Get prompt completions",
        ReadOnly = true)]
    public static async Task<CallToolResult> ChatApp_GetCompletions(
       IServiceProvider serviceProvider)
    {
        var config = serviceProvider.GetServices<IAutoCompletion>();

        return await Task.FromResult(string.Join(",", config.SelectMany(z => z.GetArguments(serviceProvider))).ToTextCallToolResponse());
    }

    [Description("Generate a very short, friendly welcome message for a chatbot interface")]
    [McpServerTool(Title = "Generate welcome message",
        ReadOnly = true)]
    public static async Task<ContentBlock?> ChatApp_GenerateWelcomeMessage(
        [Description("Language of the requested welcome message")] string language,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Current date and time")] string? currentDateTime = null,
        [Description("Current user")] string? currentUser = null,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();

        // Pick the model you want
        var modelName = "gpt-5-nano";
        // Optional: Logging/notification
        var markdown = $"Generating welcome message";

        await mcpServer.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);

        var options = new
        {
            reasoning = new { effort = "minimal" },
        };

        const int MaxLength = 60;

        var args = new Dictionary<string, JsonElement>
        {
            { "language", JsonSerializer.SerializeToElement(language) },
            { "currentDateTime", JsonSerializer.SerializeToElement(currentDateTime ?? DateTime.UtcNow.ToString()) }
        };

        if (currentUser != null)
            args.Add("currentUser", currentUser.ToJsonElement());

        var meta = new Dictionary<string, object> {
            { "openai", options },
            { "xai", new { } },
            { "google", new { } },
            { "anthropic", new { } }  };

        async Task<ContentBlock?> SampleAsync() =>
            (await samplingService.GetPromptSample(
                serviceProvider,
                mcpServer,
                "welcome-message",
                arguments: args,
                modelHint: modelName,
                metadata: meta,
                cancellationToken: cancellationToken
            ))?.Content;

        var content = await SampleAsync();
        if (content is TextContentBlock textContentBlock && textContentBlock.Text.Length > MaxLength)
        {
            // Eén retry; tweede resultaat niet opnieuw checken
            content = await SampleAsync();
        }

        return content;
    }

    [Description("Explain a tool call to an end user in simple words")]
    [McpServerTool(Title = "Explain tool call in simple words",
        ReadOnly = true)]
    public static async Task<ContentBlock> ChatApp_ExplainToolCall(
       [Description("Stringified json of all toolcall data")] string toolcall,
       [Description("Language of the requested welcome message")] string language,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();

        // Pick the model you want
        var modelName = "gpt-5-nano"; // or set to your preferred model

        // Optional: Logging/notification
        var markdown = $"Explain tool call";
        await mcpServer.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);

        var result = await samplingService.GetPromptSample(
            serviceProvider,
            mcpServer,
            "toolcall-explanation",
            arguments: new Dictionary<string, JsonElement>() {
                { "language", JsonSerializer.SerializeToElement(language) },
                { "toolcall", JsonSerializer.SerializeToElement(toolcall) }
                },
            modelHint: modelName,
            cancellationToken: cancellationToken);

        return result.Content;
    }
}


public class McpApplicationInsights
{
    public string AppId { get; set; } = default!;
    public string AppKey { get; set; } = default!;
}