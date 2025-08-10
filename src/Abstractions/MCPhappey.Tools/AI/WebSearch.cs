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
    //"gpt-4o-search-preview"
    private static readonly string[] ModelNames = ["sonar-pro", "gpt-5-mini", "gemini-2.5-flash"];
    private static readonly string[] AcademicModelNames = ["sonar-reasoning-pro", "gpt-5", "gemini-2.5-pro"];

    [Description("Web search using multiple AI models in parallel")]
    [McpServerTool(Title = "Web search (multi-model)",
        Destructive = false,
        ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> WebSearch_Execute(
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
                metadata: new Dictionary<string, object>()
                {
                    {"perplexity", new {
                        search_mode = "web"
                     } },
                    {"google", new {
                        google_search = new { },
                         thinkingConfig = new {
                            thinkingBudget = -1
                        }
                     } },
                    {"openai", new {
                        web_search_preview = new {
                            search_context_size = "medium"
                         },
                         reasoning = new {
                            effort = "low"
                         }
                     } },

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

    [Description("Academic web search using multiple AI models in parallel")]
    [McpServerTool(Title = "Academic web search (multi-model)",
        Destructive = false,
        ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> WebSearch_ExecuteAcademic(
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

        var tasks = AcademicModelNames.Select(async modelName =>
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
                    {"perplexity", new {
                        search_mode = "academic"
                     } },
                    {"google", new {
                        google_search = new { },
                        thinkingConfig = new {
                            thinkingBudget = -1
                        }
                     } },
                    {"openai", new {
                        web_search_preview = new {
                            search_context_size = "medium"
                         },
                         reasoning = new {
                            effort = "medium"
                         }
                     } }

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

