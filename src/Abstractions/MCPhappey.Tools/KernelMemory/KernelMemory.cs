using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Auth.Models;
using MCPhappey.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.KernelMemory;

public static class KernelMemory
{
    [Description("Search Microsoft Kernel Memory")]
    [McpServerTool(Name = "KernelMemory_Search", ReadOnly = true)]
    public static async Task<CallToolResult> KernelMemory_Search(
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
            return "Not authorized".ToErrorCallToolResponse();
        }

        ArgumentNullException.ThrowIfNull(memory);

        var answer = await memory.SearchAsync(query, index, minRelevance: minRelevance ?? 0,
            limit: limit ?? int.MaxValue,
            cancellationToken: cancellationToken);

        return answer.ToJsonContentBlock(index)
            .ToCallToolResult();
    }

    [Description("Ask Microsoft Kernel Memory")]
    [McpServerTool(Name = "KernelMemory_Ask", ReadOnly = true)]
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

        return answer.ToJsonContentBlock(index)
            .ToCallToolResult();
    }

    [Description("List available Microsoft Kernel Memory indexes")]
    [McpServerTool(Name = "KernelMemory_ListIndexes", ReadOnly = true)]
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

