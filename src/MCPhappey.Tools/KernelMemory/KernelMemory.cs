using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Tools.KernelMemory;

public static class KernelMemory
{
    [Description("Search Microsoft Kernel Memory")]
    public static async Task<CallToolResponse> KernelMemory_Search(
        [Description("Search query")]
        string query,
        [Description("Kernel memory index")]
        string index,
        [Description("Minimum relevance")]
        double? minRelevance,
        [Description("Limit the number of results")]
        int? limit,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(index);
        var memory = serviceProvider.GetService<IKernelMemory>();

        ArgumentNullException.ThrowIfNull(memory);

        var answer = await memory.SearchAsync(query, index, minRelevance: minRelevance ?? 0,
            limit: limit ?? int.MaxValue,
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(answer).ToJsonCallToolResponse($"kernel://{index}/search/{query}");
    }

    [Description("Ask Microsoft Kernel Memory")]
    public static async Task<CallToolResponse> KernelMemory_Ask(
        [Description("Question prompt")]
        string prompt,
        [Description("Kernel memory index")]
        string index,
        [Description("Minimum relevance")]
        double? minRelevance,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(index);
        var memory = serviceProvider.GetService<IKernelMemory>();

        ArgumentNullException.ThrowIfNull(memory);
        var answer = await memory.AskAsync(prompt, index, minRelevance: minRelevance ?? 0,
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(answer).ToJsonCallToolResponse($"kernel://{index}/ask/{prompt}");
    }
}

