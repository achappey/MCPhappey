using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.AI;

public static class WebSearch
{
    private static readonly string[] ModelNames = ["sonar-pro", "gpt-4o-search-preview", "gemini-2.5-pro-preview-06-05"];

    [Description("Web search using multiple AI models in parallel")]
    [McpServerTool(Name = "WebSearch_ExecuteWebSearch", Title = "Web search (multi-model)", ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> WebSearch_ExecuteWebSearch(
       [Description("Search query")] string query,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();

        var promptArgs = new Dictionary<string, JsonElement>
        {
            ["query"] = JsonSerializer.SerializeToElement(query)
        };

        int? progressToken = 1;

        var markdown = $"{string.Join(", ", ModelNames)}\n{query}";
        await requestContext.Server.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);

        var tasks = ModelNames.Select(async modelName =>
        {
            var markdown = $"{modelName}\n{query}";

            var result = await samplingService.GetPromptSample(
                serviceProvider,
                mcpServer,
                "ai-websearch-answer",
                promptArgs,
                modelName,
                cancellationToken: cancellationToken
            );

            progressToken = await requestContext.Server.SendProgressNotificationAsync(
                           requestContext,
                           progressToken,
                           markdown,
                           ModelNames.Length,
                           cancellationToken
                       );

            return result;
        });

        CreateMessageResult[] results = await Task.WhenAll(tasks);

        // Terug als dictionary: model => antwoord
        return results
            .Select(a => a.Content);
    }

    [Description("Academic web search using multiple AI models in parallel")]
    [McpServerTool(Name = "WebSearch_ExecuteAcademicWebSearch",
        Title = "Academic web search (multi-model)",
        ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> WebSearch_ExecuteAcademicWebSearch(
      [Description("Search query")] string query,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();

        var promptArgs = new Dictionary<string, JsonElement>
        {
            ["query"] = JsonSerializer.SerializeToElement(query)
        };

        int? progressToken = 1;

        var markdown = $"{string.Join(", ", ModelNames)}\n{query}";
        await requestContext.Server.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);

        var tasks = ModelNames.Select(async modelName =>
        {
            var markdown = $"{modelName}\n{query}";
            var result = await samplingService.GetPromptSample(
                serviceProvider,
                mcpServer,
                "ai-academic-research-answer",
                promptArgs,
                modelName,
                metadata: new Dictionary<string, object>()
                {
                    {"search_mode", "academic"}
                },
                cancellationToken: cancellationToken
            );

            progressToken = await requestContext.Server.SendProgressNotificationAsync(
                           requestContext,
                           progressToken,
                           markdown,
                           ModelNames.Length,
                           cancellationToken
                       );

            return result;
        });

        CreateMessageResult[] results = await Task.WhenAll(tasks);

        // Terug als dictionary: model => antwoord
        return results
            .Select(a => a.Content);
    }

}

