using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.OpenAI.Research;

public static class OpenAIResearch
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ImageQuality
    {
        low,
        medium,
        high,
        auto
    }


    [Description("Perform web research on a topic. Before you use this tool, always ask the user first for more details so you can craft a detailed research topic for maximum accuracy")]
    public static async Task<CallToolResult> OpenAIResearch_PerformResearch(
        [Description("Topic for the research")]
        string researchTopic,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(researchTopic);
        var uploadService = serviceProvider.GetRequiredService<UploadService>();
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();
        int? progressCounter = requestContext.Params?.ProgressToken is not null ? 1 : null;

        if (requestContext.Server.ClientCapabilities?.Sampling == null)
        {
            return "Sampling is required for this tool".ToErrorCallToolResponse();
        }

        var queryArgs = new Dictionary<string, JsonElement>()
        {
            ["query"] = JsonSerializer.SerializeToElement(researchTopic),
        };

        var querySampling = await samplingService.GetPromptSample<WebSearchPlan>(serviceProvider,
            requestContext.Server, "web-search-planner", queryArgs,
                "o3", cancellationToken: cancellationToken);
        var counter = 1;

        if (requestContext.Params?.ProgressToken is not null)
        {
            await requestContext.Server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams()
            {
                ProgressToken = requestContext.Params.ProgressToken.Value,
                Progress = new ProgressNotificationValue()
                {
                    Progress = counter,
                    Message = $"Expanded to {querySampling?.Searches.Count()} queries:\n{string.Join("\n",
                        querySampling?.Searches.Select(a => a.Query) ?? [])}"
                },
            }, cancellationToken: cancellationToken);
        }

        var queries = querySampling?.Searches.Select(a => $"- {a.Query}: {a.Reason}") ?? [];

        var total = querySampling?.Searches.Count + 2;

        var researchTasks = querySampling?.Searches.Select(a => GetWebResearch(requestContext.Server,
             samplingService, requestContext, counter++, total, serviceProvider,
             a.Query, a.Reason,
            cancellationToken));

        var searchResults = await Task.WhenAll(researchTasks ?? []);
        var resultItems = searchResults
            .OfType<string>();

        if (requestContext.Params?.ProgressToken is not null)
        {
            await requestContext.Server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams()
            {
                ProgressToken = requestContext.Params.ProgressToken.Value,
                Progress = new ProgressNotificationValue()
                {
                    Progress = counter++,
                    Total = total,
                    Message = $"Writing report"
                },
            }, cancellationToken: cancellationToken);
        }


        var reportArgs = new Dictionary<string, JsonElement>()
        {
           {"query", JsonSerializer.SerializeToElement(researchTopic)},
           {"searchResults", JsonSerializer.SerializeToElement(string.Join("\n\n", searchResults))}
        };

        var reportSampling = await samplingService.GetPromptSample<ResearchReport>(serviceProvider,
            requestContext.Server, "write-report", reportArgs,
            "gpt-4o", cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(reportSampling?.MarkdownReport))
        {
            return string.Join("\n\n", searchResults).ToTextCallToolResponse();
        }

        string fullReport = $"\n\n=====REPORT=====\n\n";
        fullReport += $"Report: {reportSampling.MarkdownReport}";
        fullReport += $"\n\n=====FOLLOW UP QUESTIONS=====\n\n";
        fullReport += $"Follow up questions: {string.Join("\n", reportSampling.FollowUpQuestions ?? [])}";

        List<ContentBlock> content = [new TextContentBlock(){
                Text = fullReport
            }];

        var uploaded = await uploadService.UploadToRoot(requestContext.Server, serviceProvider,
          $"OpenAI-Research-{DateTime.Now.Ticks}.md",
            BinaryData.FromString(fullReport), cancellationToken);

        if (uploaded != null)
        {
            content.Add(new EmbeddedResourceBlock()
            {
                Resource = new TextResourceContents()
                {
                    MimeType = uploaded.MimeType,
                    Uri = uploaded.Uri,
                    Text = JsonSerializer.Serialize(uploaded)
                }
            });
        }

        return new CallToolResult()
        {
            Content = content
        };
    }

    private static async Task<string?> GetWebResearch(IMcpServer mcpServer,
        SamplingService samplingService,
        RequestContext<CallToolRequestParams> requestContext,
        int? counter,
        int? total,
        IServiceProvider serviceProvider,
        string topic, string reason,
        CancellationToken cancellationToken = default)
    {
        if (requestContext.Params?.ProgressToken is not null)
        {
            await mcpServer.SendNotificationAsync("notifications/progress", new ProgressNotificationParams()
            {
                ProgressToken = requestContext.Params.ProgressToken.Value!,
                Progress = new ProgressNotificationValue()
                {
                    Progress = counter!.Value,
                    Total = total,
                    Message = $"Searching: {topic}\nReason: {reason}"
                },
            }, cancellationToken: CancellationToken.None);
        }

        var values = new Dictionary<string, JsonElement>()
                       {
                {"searchTerm", JsonSerializer.SerializeToElement(topic)},
                {"searchReason", JsonSerializer.SerializeToElement(reason)}
                       };

        var querySampling = await samplingService.GetPromptSample(serviceProvider,
                 mcpServer, "web-research", values,
                     "gpt-4o-search-preview", cancellationToken: cancellationToken);

        return querySampling.ToText();
    }

    public class WebSearchItem
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = null!;

        [JsonPropertyName("query")]
        public string Query { get; set; } = null!;
    }


    public class WebSearchPlan
    {
        [JsonPropertyName("queries")]
        public List<WebSearchItem> Searches { get; set; } = [];
    }


    public class ResearchReport
    {
        [JsonPropertyName("short_summary")]
        public string ShortSummary { get; set; } = null!;

        [JsonPropertyName("markdown_report")]
        public string MarkdownReport { get; set; } = null!;

        [JsonPropertyName("follow_up_questions")]
        public List<string> FollowUpQuestions { get; set; } = [];
    }


}

