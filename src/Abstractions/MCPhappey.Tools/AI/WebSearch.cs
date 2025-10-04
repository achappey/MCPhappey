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
    private static readonly string[] ModelNames = ["sonar-pro", "gpt-5-mini", "gemini-2.5-flash", "claude-3-5-haiku-latest"];
    private static readonly string[] AcademicModelNames = ["sonar-reasoning-pro", "gpt-5", "gemini-2.5-pro", "claude-opus-4-1-20250805"];

    [Description("Parallel web search across multiple AI models, optionally filtered by date range. If a date range is used, include it in the prompt, as some providers don’t support date filters.")]
    [McpServerTool(Title = "Web search (multi-model)",
        ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> WebSearch_Execute(
       [Description("Search query")] string query,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       [Description("Start date of the date range")] string? startDate = null,
       [Description("End date of the date range")] string? endDate = null,
       [Description("Search context size. low, medium or high")] string? searchContextSize = "medium",
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
        await requestContext.Server.SendMessageNotificationAsync(markdown, LoggingLevel.Debug, cancellationToken: CancellationToken.None);

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
                                    search_context_size = searchContextSize
                                },
                                last_updated_before_filter = endDate,
                                last_updated_after_filter = startDate
                            } },
                            { "google", new {
                                google_search = new { timeRangeFilter = new {
                                    startTime = startDate,
                                    endTime = endDate
                                } },
                                thinkingConfig = new {
                                    thinkingBudget = -1
                                }
                            } },
                            { "openai", new {
                                web_search = new {
                                    search_context_size = searchContextSize
                                },
                                reasoning = new {
                                    effort = "low"
                                }
                            } },
                            { "anthropic", new {
                                web_search = new {
                                    max_uses = searchContextSize == "low" ? 3 : searchContextSize == "high" ? 7 : 5
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
                        $"{modelName} failed: {ex.Message}",
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
       [Description("Start date of the date range")] string? startDate = null,
       [Description("End date of the date range")] string? endDate = null,
       [Description("Search context size. low, medium or high")] string? searchContextSize = "medium",
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
                            search_context_size = searchContextSize
                        }
                     } },
                    { "google", new {
                        google_search = new {
                            timeRangeFilter = new {
                                    startTime = startDate,
                                    endTime = endDate
                                } },
                        thinkingConfig = new {
                            thinkingBudget = -1
                        }
                     } },
                    { "openai", new {
                        web_search = new {
                            search_context_size = searchContextSize
                         },
                         reasoning = new {
                            effort = "medium"
                         }
                     } },
                    { "anthropic", new {
                        web_search = new {
                              max_uses = searchContextSize == "low"
                                ? 3 : searchContextSize == "high" ? 7 : 5
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
                    $"{modelName} failed: {ex.Message}",
                    LoggingLevel.Error
                );
                return null; // Skip failed
            }
        });

        var results = await Task.WhenAll(tasks);

        // Only keep successes
        return results.Where(r => r != null)!;
    }
}

