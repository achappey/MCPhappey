using System.ComponentModel;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using MCPhappey.Tools.Mistral.DocumentAI;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using MCPhappey.Common.Models;

namespace MCPhappey.Tools.Mistral.Libraries;

public static partial class LibrariesPlugin
{
    private const string BaseUrl = "https://api.mistral.ai/v1/libraries";


    [Description("Please confirm deletion of the library with this exact ID: {0}")]
    public class DeleteLibrary : IHasName
    {
        [JsonPropertyName("name")]
        [Description("ID of the library.")]
        public string Name { get; set; } = default!;
    }

    [Description("Delete a Mistral library by ID.")]
    [McpServerTool(
        Title = "Delete Mistral library",
        Name = "mistral_libraries_delete",
        ReadOnly = false,
        OpenWorld = false,
        Destructive = true)]
    public static async Task<CallToolResult?> MistralLibraries_Delete(
        [Description("The ID of the library to delete.")] string libraryId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
        await requestContext.WithStructuredContent(async () =>
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(libraryId);

            var mistralSettings = serviceProvider.GetRequiredService<MistralSettings>();
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var endpoint = $"https://api.mistral.ai/v1/libraries/{libraryId}";

            return await requestContext.ConfirmAndDeleteAsync<DeleteLibrary>(
                expectedName: libraryId,
                deleteAction: async _ =>
                {
                    using var client = clientFactory.CreateClient();
                    using var req = new HttpRequestMessage(HttpMethod.Delete, endpoint);

                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mistralSettings.ApiKey);
                    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    using var resp = await client.SendAsync(req, cancellationToken);
                    var json = await resp.Content.ReadAsStringAsync(cancellationToken);

                    if (!resp.IsSuccessStatusCode)
                        throw new Exception($"{resp.StatusCode}: {json}");
                },
                successText: $"Library '{libraryId}' deleted successfully!",
                ct: cancellationToken);
        }));

    [Description("Create a new Mistral library that can be used in conversations.")]
    [McpServerTool(
        Title = "Create Mistral library",
        Name = "mistral_libraries_create",
        ReadOnly = false,
        OpenWorld = false,
        Destructive = false)]
    public static async Task<CallToolResult?> MistralLibraries_Create(
        [Description("Name of the new library.")] string name,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Optional description of the library.")] string? description = null,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
        await requestContext.WithStructuredContent(async () =>
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name);

            var mistralSettings = serviceProvider.GetRequiredService<MistralSettings>();
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
                new CreateLibrary
                {
                    Name = name,
                    Description = description
                },
                cancellationToken
            );

            if (notAccepted != null) throw new Exception(JsonSerializer.Serialize(notAccepted));
            if (typed == null) throw new Exception("Invalid library input");

            var body = new Dictionary<string, object?>
            {
                ["name"] = typed.Name,
                ["description"] = typed.Description
            };

            using var client = clientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(body),
                                           Encoding.UTF8,
                                           "application/json")
            };

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mistralSettings.ApiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await client.SendAsync(req, cancellationToken);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{resp.StatusCode}: {json}");

            return json.ToJsonCallToolResponse($"{BaseUrl}");
        }));

    public class CreateLibrary
    {
        [Required]
        [JsonPropertyName("name")]
        [Description("The name of the library.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("description")]
        [Description("Optional description of the library.")]
        public string? Description { get; set; }
    }
}
