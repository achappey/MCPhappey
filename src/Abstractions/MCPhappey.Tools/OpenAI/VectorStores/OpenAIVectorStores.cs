using System.ClientModel;
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
using OAIV = OpenAI.VectorStores;

namespace MCPhappey.Tools.OpenAI.VectorStores;

public static class OpenAIVectorStores
{
    [Description("Create a vector store at OpenAI")]
    [McpServerTool(Title = "Create a vector store at OpenAI", Destructive = false)]
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

        var item = await openAiClient
            .GetVectorStoreClient()
            .CreateVectorStoreAsync(typed.WaitUntilCompleted, options, cancellationToken);

        return item?.ToJsonContentBlock($"https://api.openai.com/v1/vector_stores/{item.VectorStoreId}").ToCallToolResult();
    }

    [Description("Delete a vector store at OpenAI")]
    [McpServerTool(Title = "Delete a vector store at OpenAI")]
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
    [McpServerTool(Title = "Search a vector store at OpenAI")]
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

        if (userId == null || !item.Value.Metadata.ContainsKey("Owners") || !item.Value.Metadata["Owners"].Contains(userId))
        {
            return "Only owners can search in a vector store".ToErrorCallToolResponse();
        }

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

    [Description("Please fill in the vector store details.")]
    public class OpenAINewVectorStore
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The vector store name.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("waitUntilCompleted")]
        [DefaultValue(true)]
        [Required]
        [Description("Wait until completed.")]
        public bool WaitUntilCompleted { get; set; }
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

