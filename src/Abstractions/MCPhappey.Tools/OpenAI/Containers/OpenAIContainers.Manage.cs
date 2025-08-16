using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ClientModel;

namespace MCPhappey.Tools.OpenAI.Containers;

public static partial class OpenAIContainers
{
    [Description("Create a container at OpenAI")]
    [McpServerTool(Title = "Create a container", Destructive = false, OpenWorld = false)]
    public static async Task<CallToolResult?> OpenAIContainers_Create(
        [Description("The container name.")] string name,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
        =>
        await requestContext.WithExceptionCheck(async () =>
        await serviceProvider.WithContainerClient(async (client) =>
        {
            var userId = serviceProvider.GetUserId();

            var imageInput = new OpenAINewContainer
            {
                Name = name
            };

            var (typed, notAccepted, result) = await requestContext.Server.TryElicit(imageInput, cancellationToken);
            if (notAccepted != null) return notAccepted;
            if (typed == null) return "Error".ToErrorCallToolResponse();

            var payload = new Dictionary<string, object?>
            {
                ["name"] = typed.Name,
            };

            var content = BinaryContent.Create(BinaryData.FromObjectAsJson(payload));
            var item = await client.CreateContainerAsync(content);
            using var raw = item.GetRawResponse();            // PipelineResponse
            string json = raw.Content.ToString();

            return json?.ToJsonContentBlock($"{ContainerExtensions.BASE_URL}").ToCallToolResult();
        }));

    [Description("Please fill in the container details.")]
    public class OpenAINewContainer
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The container name.")]
        public string Name { get; set; } = default!;

    }

}

