using System.ClientModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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

public static class OpenAIVectorStores
{
    public static bool IsOwner(this VectorStore store, string? userId)
        => userId != null && store.Metadata.ContainsKey("Owners") && store.Metadata["Owners"].Contains(userId);

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
        current.Value.Metadata.TryGetValue("Description", out var currentDescription);

        // Prepare elicitation payload with defaults from method params (fallback to current)
        var input = new OpenAIEditVectorStore
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name! : currentName,
            Description = description ?? currentDescription
        };

        var (typed, notAccepted) = await requestContext.Server.TryElicit(input, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Error".ToErrorCallToolResponse();

        // Build update options; preserve existing metadata (Owners/Visibility/etc.)
        var newMetadata = new Dictionary<string, string>(current.Value.Metadata);
        if (typed.Description != null)
            newMetadata["Description"] = typed.Description;

        // SDK naming differs by version; both are common. Use the one your package exposes.
        var updateOptions = new OAIV.VectorStoreModificationOptions
        {
            Name = typed.Name,
        };

        if (!string.IsNullOrEmpty(typed.Description))
            updateOptions.Metadata.Add("Description", typed.Description);
        else
            updateOptions.Metadata.Add("Description", string.Empty);

        foreach (var i in current.Value.Metadata
            .Where(z => !updateOptions.Metadata.ContainsKey(z.Key)))
        {
            updateOptions.Metadata.Add(i.Value, i.Key);
        }

        var updated = await client.ModifyVectorStoreAsync(vectorStoreId, updateOptions, cancellationToken);
        
        return updated?.ToJsonContentBlock($"https://api.openai.com/v1/vector_stores/{vectorStoreId}").ToCallToolResult();
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

        var (typed, notAccepted) = await requestContext.Server.TryElicit(imageInput, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Error".ToErrorCallToolResponse();

        var options = new OAIV.VectorStoreCreationOptions()
        {
            Name = typed.Name
        };

        options.Metadata.Add("Owners", userId!);
        options.Metadata.Add("Visibility", "Owners");

        if (!string.IsNullOrEmpty(typed.Description))
            options.Metadata.Add("Description", typed.Description);

        var item = await openAiClient
            .GetVectorStoreClient()
            .CreateVectorStoreAsync(typed.WaitUntilCompleted, options, cancellationToken);

        return item?.ToJsonContentBlock($"https://api.openai.com/v1/vector_stores/{item.VectorStoreId}").ToCallToolResult();
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

        if (userId == null || !item.Value.Metadata.ContainsKey("Owners") || !item.Value.Metadata["Owners"].Contains(userId))
        {
            return "Only owners can delete a vector store".ToErrorCallToolResponse();
        }

        return await requestContext.ConfirmAndDeleteAsync<OpenAIDeleteVectorStore>(
                   item?.Value.Name!,
                   async _ => await client.DeleteVectorStoreAsync(vectorStoreId, cancellationToken),
            $"Vector store {item?.Value.Name} deleted.",
            cancellationToken);
    }

    [Description("Search a vector store for relevant chunks based on a query.")]
    [McpServerTool(Title = "Search a vector store at OpenAI", ReadOnly = true,
        Idempotent = true,
        OpenWorld = false,
        Destructive = false)]
    public static async Task<CallToolResult?> OpenAIVectorStores_Search(
          [Description("The vector store id.")] string vectorStoreId,
          [Description("The vector store prompt query.")] string query,
          IServiceProvider serviceProvider,
          [Description("If the query should be rewritten.")] bool? rewriteQuery = false,
          [Description("Maximum number of results.")] int? maxNumOfResults = 10,
          CancellationToken cancellationToken = default)
    {
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();
        var userId = serviceProvider.GetUserId();
        var client = openAiClient
                    .GetVectorStoreClient();

        var item = client
                    .GetVectorStore(vectorStoreId, cancellationToken);

        if (!item.Value.IsOwner(userId))
            return "Only owners can search a vector store".ToErrorCallToolResponse();

        var payload = new Dictionary<string, object?>
        {
            ["query"] = query,        // or: new[] { "q1", "q2" }
            ["max_num_results"] = maxNumOfResults ?? 10,                         // optional (1..50)
            ["rewrite_query"] = rewriteQuery ?? false                          // optional
                                                                               // ["ranking_options"] = new Dictionary<string, object?> { /* ... */ } // optional
        };

        var content = BinaryContent.Create(BinaryData.FromObjectAsJson(payload));
        var searchResult = await client
            .SearchVectorStoreAsync(vectorStoreId, content);
        var raw = searchResult.GetRawResponse();            // PipelineResponse
        string json = raw.Content.ToString();         // JSON string

        return json.ToJsonCallToolResponse($"https://api.openai.com/v1/vector_stores/{vectorStoreId}/search");
    }

    [Description("Ask a question against an OpenAI vector store using file_search via sampling.")]
    [McpServerTool(
           Title = "Ask OpenAI vector store",
           Destructive = false,
           ReadOnly = true)]
    public static async Task<CallToolResult> OpenAIVectorStores_Ask(
           [Description("The OpenAI vector store id.")] string vectorStoreId,
           [Description("Your question / query.")] string query,
           IServiceProvider serviceProvider,
           RequestContext<CallToolRequestParams> requestContext,
           [Description("Optional model override (defaults to gpt-5).")] string? model = "gpt-5",
           [Description("Max number of retrieved chunks.")] int? maxNumResults = 10,
           CancellationToken cancellationToken = default)
    {
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();
        var userId = serviceProvider.GetUserId();
        var client = openAiClient
                    .GetVectorStoreClient();

        var item = client
                    .GetVectorStore(vectorStoreId, cancellationToken);

        if (!item.Value.IsOwner(userId))
            return "Only owners can ask a vector store".ToErrorCallToolResponse();

        var respone = await requestContext.Server.SampleAsync(new CreateMessageRequestParams()
        {
            Metadata = JsonSerializer.SerializeToElement(new Dictionary<string, object>()
                {
                    {"openai", new {
                        file_search = new
                        {
                            vector_store_ids = new[] { vectorStoreId },
                            max_num_results = maxNumResults ?? 10
                        },
                        reasoning = new
                        {
                            effort = "low"
                        }
                        } },
                        }),
            Temperature = 1,
            MaxTokens = 8192,
            ModelPreferences = model.ToModelPreferences(),
            Messages = [query.ToUserSamplingMessage()]
        }, cancellationToken);

        // Return the model’s final content blocks
        return respone.Content.ToCallToolResult();
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

}

