using System.ComponentModel;
using MCPhappey.Auth.Models;
using MCPhappey.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.KernelMemory;

public static class KernelMemory
{
    private static Common.Models.SearchResult ToSearchResult(this Citation citation)
        => new()
        {
            Title = citation.SourceName,
            Source = citation.SourceUrl!,
            Content = citation.Partitions.Select(a => new Common.Models.SearchResultContentBlock()
            {
                Text = a.Text
            }),
            Citations = new Common.Models.CitationConfiguration()
            {
                Enabled = true
            }
        };

    [Description("Search Microsoft Kernel Memory")]
    [McpServerTool(Title = "Search Microsoft kernel memory",
        ReadOnly = true, Destructive = false, Idempotent = true, UseStructuredContent = true)]
    public static async Task<IEnumerable<Common.Models.SearchResult>> KernelMemory_Search(
        [Description("Search query")]
        string query,
        [Description("Kernel memory index")]
        string index,
        IServiceProvider serviceProvider,
        [Description("Minimum relevance")]
        double? minRelevance = 0,
        [Description("Limit the number of results")]
        int? limit = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(index);
        var memory = serviceProvider.GetService<IKernelMemory>();
        var appSettings = serviceProvider.GetService<OAuthSettings>();
        if (appSettings?.ClientId.Equals(index, StringComparison.OrdinalIgnoreCase) == true)
        {
            //         return "Not authorized".ToErrorCallToolResponse();
            throw new UnauthorizedAccessException();
        }

        ArgumentNullException.ThrowIfNull(memory);

        var answer = await memory.SearchAsync(query, index, minRelevance: minRelevance ?? 0,
            limit: limit ?? int.MaxValue,
            cancellationToken: cancellationToken);

        return answer.Results.Select(b => b.ToSearchResult());
        /*
                return new
                {
                    answer.Query,
                    answer.NoResult,
                    Results = answer.Results.Select(b => new
                    {
                        b.SourceUrl,
                        b.Partitions.OrderByDescending(y => y.LastUpdate).FirstOrDefault()?.LastUpdate,
                        Citations = b.Partitions.Select(z => z.Text)
                    })
                }.ToJsonContentBlock(index)
                .ToCallToolResult();*/
    }

    [Description("Ask Microsoft Kernel Memory")]
    [McpServerTool(Title = "Ask Microsoft kernel memory",
        ReadOnly = true, Destructive = false)]
    public static async Task<CallToolResult> KernelMemory_Ask(
        [Description("Question prompt")]
        string prompt,
        [Description("Kernel memory index")]
        string index,
        IServiceProvider serviceProvider,
        [Description("Minimum relevance")]
        double? minRelevance = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(index);
        var memory = serviceProvider.GetService<IKernelMemory>();
        var appSettings = serviceProvider.GetService<OAuthSettings>();
        if (appSettings?.ClientId.Equals(index, StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Not authorized".ToErrorCallToolResponse();
        }

        ArgumentNullException.ThrowIfNull(memory);
        var answer = await memory.AskAsync(prompt, index, minRelevance: minRelevance ?? 0,
            cancellationToken: cancellationToken);

        return new
        {
            answer.Question,
            answer.NoResult,
            Text = answer.Result,
            RelevantSources = answer.RelevantSources.Select(b => new
            {
                b.SourceUrl,
                b.Partitions.OrderByDescending(y => y.LastUpdate).FirstOrDefault()?.LastUpdate,
                Citations = b.Partitions.Select(z => z.Text)
            })
        }.ToJsonContentBlock(index)
                    .ToCallToolResult();
    }

    [Description("List available Microsoft Kernel Memory indexes")]
    [McpServerTool(Title = "List kernel memory indexes", Idempotent = true,
        ReadOnly = true, Destructive = false, OpenWorld = false)]
    public static async Task<CallToolResult> KernelMemory_ListIndexes(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var memory = serviceProvider.GetService<IKernelMemory>();
        var appSettings = serviceProvider.GetService<OAuthSettings>();
        ArgumentNullException.ThrowIfNull(memory);
        var indexes = await memory.ListIndexesAsync(cancellationToken: cancellationToken);

        return indexes.Where(a => a.Name != appSettings?.ClientId)
            .ToJsonContentBlock("kernel://list")
            .ToCallToolResult();
    }
}

