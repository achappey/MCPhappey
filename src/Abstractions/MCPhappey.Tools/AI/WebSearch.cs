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
    private static readonly string[] ModelNames = ["sonar-pro", "gpt-5-mini", "gemini-2.5-flash", "claude-sonnet-4-20250514"];
    private static readonly string[] AcademicModelNames = ["sonar-reasoning-pro", "gpt-5", "gemini-2.5-pro", "claude-sonnet-4-20250514"];

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
                try
                {
                    var markdown = $"{modelName}\n{query}";

                    var result = await samplingService.GetPromptSample(
                        serviceProvider,
                        mcpServer,
                        "ai-websearch-answer",
                        promptArgs,
                        modelName,
                        metadata: new Dictionary<string, object>
                        {
                            { "perplexity", new {
                                search_mode = "web",
                                web_search_options = new {
                                    search_context_size = "medium"
                                }
                            } },
                            { "google", new {
                                google_search = new { },
                                thinkingConfig = new {
                                    thinkingBudget = -1
                                }
                            } },
                            { "openai", new {
                                web_search_preview = new {
                                    search_context_size = "medium"
                                },
                                reasoning = new {
                                    effort = "low"
                                }
                            } },
                            { "anthropic", new {
                                web_search = new {
                                    max_uses = 5
                                },
                                thinking = new {
                                    budget_tokens = 4096
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

                    return result.Content; // Success
                }
                catch (Exception ex)
                {
                    await requestContext.Server.SendMessageNotificationAsync(
                        $"⚠ {modelName} failed: {ex.Message}",
                        LoggingLevel.Error
                    );
                    return null; // Failure → skip
                }
            });

        var results = await Task.WhenAll(tasks);

        // Return only successful results
        return results.Where(r => r != null)!;

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

        var markdown = $"{string.Join(", ", AcademicModelNames)}\n{query}";
        await requestContext.Server.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);

        var tasks = AcademicModelNames.Select(async modelName =>
        {
            try
            {
                var markdown = $"{modelName}\n{query}";

                var result = await samplingService.GetPromptSample(
                    serviceProvider,
                    mcpServer,
                    "ai-academic-research-answer",
                    promptArgs,
                    modelName,
                    metadata: new Dictionary<string, object>
                    {
                    { "perplexity", new {
                        search_mode = "academic",
                        web_search_options = new {
                            search_context_size = "medium"
                        }
                     } },
                    { "google", new {
                        google_search = new { },
                        thinkingConfig = new {
                            thinkingBudget = -1
                        }
                     } },
                    { "openai", new {
                        web_search_preview = new {
                            search_context_size = "low"
                         },
                         reasoning = new {
                            effort = "medium"
                         }
                     } },
                    { "anthropic", new {
                        web_search = new {
                            max_uses = 5
                         },
                         thinking = new {
                            budget_tokens = 8192
                         }
                     } },
                    },
                    cancellationToken: cancellationToken
                );

                progressToken = await requestContext.Server.SendProgressNotificationAsync(
                    requestContext,
                    progressToken,
                    markdown,
                    AcademicModelNames.Length,
                    cancellationToken
                );

                return result.Content; // Success
            }
            catch (Exception ex)
            {
                await requestContext.Server.SendMessageNotificationAsync(
                    $"⚠ {modelName} failed: {ex.Message}",
                    LoggingLevel.Warning
                );
                return null; // Skip failed
            }
        });

        var results = await Task.WhenAll(tasks);

        // Only keep successes
        return results.Where(r => r != null)!;
    }
}

