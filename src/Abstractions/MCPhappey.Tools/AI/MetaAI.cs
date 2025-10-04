using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.AI;

public static class MetaAI
{
    private static readonly string[] ModelNames = ["sonar-pro", "gpt-5-mini", "gemini-2.5-flash",
            "claude-3-5-haiku-latest", "mistral-medium-latest"];

    [Description("Ask once, answer from many. Sends the same prompt to multiple AI providers in parallel and returns their answers.")]
    [McpServerTool(
       Title = "Ask (multi-model)",
       ReadOnly = true
   )]
    public static async Task<IEnumerable<ContentBlock>> Ask_Execute(
       [Description("User prompt or question")] string prompt,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();

        // Progress + logging
        int? progressToken = 1;
        await mcpServer.SendMessageNotificationAsync(
            $"Meta-AI: {string.Join(", ", ModelNames)}\n{prompt}",
            LoggingLevel.Debug,
            cancellationToken: CancellationToken.None
        );

        // Optional: per-provider generation hints (no web_search here)
        var metadata = new Dictionary<string, object?>
        {
            ["openai"] = new
            {
                reasoning = new
                {
                    effort = "low"
                }
            },
            ["anthropic"] = new
            {
                thinking = new
                {
                    budget_tokens = 4096
                }
            },
            ["google"] = new
            {
                thinkingConfig = new
                {
                    thinkingBudget = -1
                }
            },
            ["perplexity"] = new
            {
                search_mode = "web",
            },
            ["mistral"] = new
            {

            }
        };

        // Parallel calls per model
        var tasks = ModelNames.Select(async modelName =>
        {
            try
            {
                // Use your general (non-search) prompt id
                var result = await requestContext.Server.SampleAsync(new CreateMessageRequestParams()
                {
                    IncludeContext = ContextInclusion.AllServers,
                    MaxTokens = 4096,
                    ModelPreferences = modelName?.ToModelPreferences(),
                    Temperature = 1,
                    Metadata = JsonSerializer.SerializeToElement(metadata),
                    Messages = [new SamplingMessage() {
                        Role = Role.User,
                        Content = new TextContentBlock() {
                            Text = prompt
                        }
                     } ]
                });

                // Progress tick
                progressToken = await mcpServer.SendProgressNotificationAsync(
                    requestContext,
                    progressToken,
                    $"{modelName} ✓",
                    ModelNames.Length,
                    cancellationToken
                );

                // Prepend a small header so de UI het model kan onderscheiden
                var blocks = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"### {modelName}" }
                };

                if (result.Content is { } contentBlocks)
                    blocks.AddRange(contentBlocks);

                return blocks.AsEnumerable();
            }
            catch (Exception ex)
            {
                await mcpServer.SendMessageNotificationAsync(
                    $"{modelName} failed: {ex.Message}",
                    LoggingLevel.Error
                );
                return null; // Skip failed
            }
        });

        var results = await Task.WhenAll(tasks);

        // Flatten only successful results
        return [.. results
            .Where(r => r != null)
            .SelectMany(r => r!)];
    }
}

