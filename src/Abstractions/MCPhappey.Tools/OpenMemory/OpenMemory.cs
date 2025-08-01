using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Auth.Extensions;
using MCPhappey.Auth.Models;
using MCPhappey.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.OpenMemory;

public static class OpenMemory
{
    public const string MemoryPurpose = "Memory";

    [Description("Save a personal user memory")]
    [McpServerTool(Name = "OpenMemory_SaveMemory", OpenWorld = false)]
    public static async Task<CallToolResult> OpenMemory_SaveMemory(
        [Description("Memory to save")]
        string memory,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var kernelMemory = serviceProvider.GetRequiredService<IKernelMemory>();
        var appSettings = serviceProvider.GetService<OAuthSettings>();
        ArgumentNullException.ThrowIfNull(memory);
        var userId = serviceProvider.GetUserId();
        var tagCollections = new TagCollection
        {
            { MemoryPurpose, userId }
        };

        var (typed, notAccepted) = await requestContext.Server.TryElicit(
                new OpenMemoryNewMemory
                {
                    Memory = memory,
                },
                cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid response".ToErrorCallToolResponse();

        var answer = await kernelMemory.ImportTextAsync(typed.Memory, index: appSettings?.ClientId!,
            tags: tagCollections,
            cancellationToken: cancellationToken);

        return answer.ToTextCallToolResponse();
    }

    [Description("Delete a personal user memory")]
    [McpServerTool(Name = "OpenMemory_DeleteMemory", OpenWorld = false)]
    public static async Task<CallToolResult> OpenMemory_DeleteMemory(
        [Description("Id of the memory")]
        string memoryId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var kernelMemory = serviceProvider.GetRequiredService<IKernelMemory>();
        var appSettings = serviceProvider.GetRequiredService<OAuthSettings>();
        var userId = serviceProvider.GetUserId();
        var tagCollections = new TagCollection
        {
            { MemoryPurpose, userId }
        };

        await kernelMemory.DeleteDocumentAsync(memoryId, index: appSettings?.ClientId!,
            cancellationToken: cancellationToken);

        return "Memory deleted".ToTextCallToolResponse();
    }

    [Description("Ask a question to personal user memory")]
    [McpServerTool(Name = "OpenMemory_AskMemory", ReadOnly = true)]
    public static async Task<CallToolResult> OpenMemory_AskMemory(
        [Description("Question prompt")]
        string prompt,
        IServiceProvider serviceProvider,
        [Description("Minimum relevance")]
        double? minRelevance = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var appSettings = serviceProvider.GetService<OAuthSettings>();
        ArgumentNullException.ThrowIfNullOrWhiteSpace(appSettings?.ClientId);
        var userId = serviceProvider.GetUserId();
        var memory = serviceProvider.GetService<IKernelMemory>();
        var memFilter = new MemoryFilter
        {
            { MemoryPurpose, userId }
        };
        ArgumentNullException.ThrowIfNull(memory);
        var answer = await memory.AskAsync(prompt, appSettings.ClientId, minRelevance: minRelevance ?? 0,
            filter: memFilter,
            cancellationToken: cancellationToken);

        return new
        {
            answer.Question,
            answer.NoResult,
            Text = answer.Result,
            RelevantMemories = answer.RelevantSources.Select(b => new
            {
                Date = b.Partitions.OrderByDescending(y => y.LastUpdate).FirstOrDefault()?.LastUpdate,
                Memory = string.Join("\n\n", b.Partitions.Select(z => z.Text))
            })
        }.ToJsonContentBlock(appSettings.ClientId)
            .ToCallToolResult();
    }

    [Description("Search personal user memories with a prompt")]
    [McpServerTool(Name = "OpenMemory_SearchMemories", ReadOnly = true)]
    public static async Task<CallToolResult> OpenMemory_SearchMemories(
      [Description("Question prompt")]
        string prompt,
      IServiceProvider serviceProvider,
      [Description("Minimum relevance")]
        double? minRelevance = 0,
      [Description("Limit items")]
        int? limit = 10,
      CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var appSettings = serviceProvider.GetService<OAuthSettings>();
        ArgumentNullException.ThrowIfNullOrWhiteSpace(appSettings?.ClientId);
        var userId = serviceProvider.GetUserId();
        var memory = serviceProvider.GetService<IKernelMemory>();
        var memFilter = new MemoryFilter
        {
            { MemoryPurpose, userId }
        };

        ArgumentNullException.ThrowIfNull(memory);
        var answer = await memory.SearchAsync(prompt, appSettings.ClientId, minRelevance: minRelevance ?? 0,
            filter: memFilter,
            limit: limit ?? int.MaxValue,
            cancellationToken: cancellationToken);

        return answer.Results.Select(a => new
        {
            id = a.DocumentId,
            memory = string.Join("\n\n", a.Partitions.Select(t => t.Text))
        })
        .ToJsonContentBlock(appSettings.ClientId)
        .ToCallToolResult();
    }

    [Description("List personal user memories")]
    [McpServerTool(Name = "OpenMemory_ListMemories", ReadOnly = true)]
    public static async Task<CallToolResult> OpenMemory_ListMemories(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var memory = serviceProvider.GetService<IKernelMemory>();

        ArgumentNullException.ThrowIfNull(memory);
        var appSettings = serviceProvider.GetService<OAuthSettings>();
        ArgumentNullException.ThrowIfNullOrWhiteSpace(appSettings?.ClientId);
        var userId = serviceProvider.GetUserId();
        var memFilter = new MemoryFilter
        {
            { MemoryPurpose, userId }
        };

        var indexes = await memory.SearchAsync("*", index: appSettings.ClientId, filter: memFilter,
            limit: int.MaxValue,
            cancellationToken: cancellationToken);

        return indexes.Results.Select(a => new
        {
            id = a.DocumentId,
            memory = string.Join("\n\n", a.Partitions.Select(t => t.Text))
        })
        .ToJsonContentBlock(appSettings.ClientId)
        .ToCallToolResult();
    }


    [Description("Please fill in the new memory details.")]
    public class OpenMemoryNewMemory
    {
        [JsonPropertyName("memory")]
        [Required]
        [Description("The memory to add.")]
        public string Memory { get; set; } = default!;

    }

}

