using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenAI;
using OpenAI.VectorStores;
using OAIV = OpenAI.VectorStores;

namespace MCPhappey.Tools.OpenAI.VectorStores;

public static partial class OpenAIVectorStores
{
    public const string BASE_URL = "https://api.openai.com/v1/vector_stores";
    public const string OWNERS_KEY = "Owners";
    public const string DESCRIPTION_KEY = "Description";

    public static bool IsOwner(this VectorStore store, string? userId)
        => userId != null && store.Metadata.ContainsKey(OWNERS_KEY) && store.Metadata[OWNERS_KEY].Contains(userId);

    [Description("Update a vector store at OpenAI")]
    [McpServerTool(
       Title = "Update a vector store at OpenAI",
       Destructive = false,
       OpenWorld = false)]
    public static async Task<CallToolResult?> OpenAIVectorStores_Update(
       [Description("The vector store id.")] string vectorStoreId,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       [Description("New name (defaults to current).")] string? name = null,
       [Description("New description (defaults to current).")] string? description = null,
       CancellationToken cancellationToken = default)
    {
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();
        var userId = serviceProvider.GetUserId();
        var client = openAiClient.GetVectorStoreClient();

        // Load current store for defaults + owner check
        var current = client.GetVectorStore(vectorStoreId, cancellationToken);

        if (!current.Value.IsOwner(userId))
            return "Only owners can update a vector store".ToErrorCallToolResponse();

        // Current values
        var currentName = current.Value.Name;
        current.Value.Metadata.TryGetValue(DESCRIPTION_KEY, out var currentDescription);

        // Prepare elicitation payload with defaults from method params (fallback to current)
        var input = new OpenAIEditVectorStore
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name! : currentName,
            Description = description ?? currentDescription
        };

        var (typed, notAccepted, result) = await requestContext.Server.TryElicit(input, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Error".ToErrorCallToolResponse();

        // Build update options; preserve existing metadata (Owners/Visibility/etc.)
        var newMetadata = new Dictionary<string, string>(current.Value.Metadata);
        if (typed.Description != null)
            newMetadata[DESCRIPTION_KEY] = typed.Description;

        // SDK naming differs by version; both are common. Use the one your package exposes.
        var updateOptions = new VectorStoreModificationOptions
        {
            Name = typed.Name,
        };

        if (!string.IsNullOrEmpty(typed.Description))
            updateOptions.Metadata.Add(DESCRIPTION_KEY, typed.Description);
        else
            updateOptions.Metadata.Add(DESCRIPTION_KEY, string.Empty);

        foreach (var i in current.Value.Metadata
            .Where(z => !updateOptions.Metadata.ContainsKey(z.Key)))
        {
            updateOptions.Metadata.Add(i.Value, i.Key);
        }

        var updated = await client.ModifyVectorStoreAsync(vectorStoreId, updateOptions, cancellationToken);

        return updated?.ToJsonContentBlock($"{BASE_URL}/{vectorStoreId}")
            .ToCallToolResult();
    }

    [Description("Add an owner to an OpenAI vector store")]
    [McpServerTool(
      Title = "Add vector store owner",
      Destructive = false,
      OpenWorld = false)]
    public static async Task<CallToolResult?> OpenAIVectorStores_AddOwner(
      [Description("The vector store id.")] string vectorStoreId,
      [Description("The user id of the new owner.")] string ownerId,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default)
    {
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();
        var userId = serviceProvider.GetUserId();
        var client = openAiClient.GetVectorStoreClient();

        // Load current store for defaults + owner check
        var current = client.GetVectorStore(vectorStoreId, cancellationToken);

        if (!current.Value.IsOwner(userId))
            return "Only owners can update a vector store".ToErrorCallToolResponse();

        // Current values
        var currentName = current.Value.Name;
        current.Value.Metadata.TryGetValue(OWNERS_KEY, out var currentDescription);

        // Prepare elicitation payload with defaults from method params (fallback to current)
        var input = new OpenAIAddVectorStoreOwner
        {
            UserId = ownerId
        };

        var (typed, notAccepted, result) = await requestContext.Server.TryElicit(input, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Error".ToErrorCallToolResponse();

        // Build update options; preserve existing metadata (Owners/Visibility/etc.)
        var updateOptions = new VectorStoreModificationOptions
        {

        };

        var currentOwners = currentDescription?.Split(",")?.ToList() ?? [];

        if (!string.IsNullOrEmpty(typed.UserId) && !currentOwners.Contains(typed.UserId))
            currentOwners.Add(typed.UserId);

        updateOptions.Metadata.Add(OWNERS_KEY, string.Join(",", currentOwners));

        foreach (var i in current.Value.Metadata
            .Where(z => !updateOptions.Metadata.ContainsKey(z.Key)))
        {
            updateOptions.Metadata.Add(i.Value, i.Key);
        }

        var updated = await client.ModifyVectorStoreAsync(vectorStoreId, updateOptions, cancellationToken);

        return updated?.ToJsonContentBlock($"{BASE_URL}/{vectorStoreId}")
            .ToCallToolResult();
    }

    [Description("Create a vector store at OpenAI")]
    [McpServerTool(Title = "Create a vector store at OpenAI", Destructive = false, OpenWorld = false)]
    public static async Task<CallToolResult?> OpenAIVectorStores_Create(
        [Description("The vector store name.")] string name,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();
        var userId = serviceProvider.GetUserId();

        var imageInput = new OpenAINewVectorStore
        {
            Name = name,
            WaitUntilCompleted = true,
        };

        var (typed, notAccepted, result) = await requestContext.Server.TryElicit(imageInput, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Error".ToErrorCallToolResponse();

        var options = new OAIV.VectorStoreCreationOptions()
        {
            Name = typed.Name
        };

        options.Metadata.Add(OWNERS_KEY, userId!);
        options.Metadata.Add("Visibility", OWNERS_KEY);

        if (!string.IsNullOrEmpty(typed.Description))
            options.Metadata.Add(DESCRIPTION_KEY, typed.Description);

        var item = await openAiClient
            .GetVectorStoreClient()
            .CreateVectorStoreAsync(typed.WaitUntilCompleted, options, cancellationToken);

        return item?.ToJsonContentBlock($"{BASE_URL}/{item.VectorStoreId}").ToCallToolResult();
    }

    [Description("Delete a vector store at OpenAI")]
    [McpServerTool(Title = "Delete a vector store at OpenAI",
        OpenWorld = false)]
    public static async Task<CallToolResult?> OpenAIVectorStores_Delete(
        [Description("The vector store id.")] string vectorStoreId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();
        var userId = serviceProvider.GetUserId();
        var client = openAiClient
                    .GetVectorStoreClient();
        var item = client
            .GetVectorStore(vectorStoreId, cancellationToken);

        if (userId == null || !item.Value.Metadata.ContainsKey(OWNERS_KEY) || !item.Value.Metadata[OWNERS_KEY].Contains(userId))
        {
            return "Only owners can delete a vector store".ToErrorCallToolResponse();
        }

        return await requestContext.ConfirmAndDeleteAsync<OpenAIDeleteVectorStore>(
                   item?.Value.Name!,
                   async _ => await client.DeleteVectorStoreAsync(vectorStoreId, cancellationToken),
            $"Vector store {item?.Value.Name} deleted.",
            cancellationToken);
    }

    [Description("Please fill in the vector store details.")]
    public class OpenAINewVectorStore
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The vector store name.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("description")]
        [Description("The vector description.")]
        public string? Description { get; set; }

        [JsonPropertyName("waitUntilCompleted")]
        [DefaultValue(true)]
        [Required]
        [Description("Wait until completed.")]
        public bool WaitUntilCompleted { get; set; }
    }

    [Description("Please fill in the vector store details.")]
    public class OpenAIEditVectorStore
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The vector store name.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("description")]
        [Description("The vector description.")]
        public string? Description { get; set; }
    }

    [Description("Please fill in the vector store details.")]
    public class OpenAIDeleteVectorStore : IHasName
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The vector store name.")]
        public string Name { get; set; } = default!;
    }

    [Description("Please fill in the vector store owner details.")]
    public class OpenAIAddVectorStoreOwner
    {
        [JsonPropertyName("userId")]
        [Required]
        [Description("The user id of the new owner.")]
        public string UserId { get; set; } = string.Empty;
    }
}

