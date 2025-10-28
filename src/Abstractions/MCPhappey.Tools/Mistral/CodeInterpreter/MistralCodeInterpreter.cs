using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Tools.OpenAI.Containers;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Mistral.CodeInterpreter;

public static class MistralCodeInterpreter
{
    [Description("Run a prompt with Mistral Code interpreter tool.")]
    [McpServerTool(Title = "Mistral Code Interpreter", Name = "mistral_codeinterpreter_run",
        Destructive = false,
        ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> MistralCodeInterpreter_Run(
            IServiceProvider serviceProvider,
          [Description("Prompt to execute (code is allowed).")]
            string prompt,
          RequestContext<CallToolRequestParams> requestContext,
          [Description("Target model (e.g. mistral-large-latest or mistral-medium-latest).")]
            string model = "mistral-medium-latest",
          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

        var respone = await requestContext.Server.SampleAsync(new CreateMessageRequestParams()
        {
            Metadata = JsonSerializer.SerializeToElement(new Dictionary<string, object>()
                {
                    {"mistral", new {
                        code_interpreter = new { type = "code_interpreter" }
                     } },
                }),
            Temperature = 0,
            MaxTokens = 8192,
            ModelPreferences = model.ToModelPreferences(),
            Messages = [prompt.ToUserSamplingMessage()]
        }, cancellationToken);

        var metadata = new EmbeddedResourceBlock()
        {
            Resource = new TextResourceContents()
            {
                Text = JsonSerializer.Serialize(respone.Meta),
                Uri = "https://api.mistral.ai",
                MimeType = "application/json"
            }
        };

        if (respone.Content is EmbeddedResourceBlock embeddedResourceBlock
            && embeddedResourceBlock.Resource is BlobResourceContents blobResourceContents)
        {
            var FileExtensionContentTypeProvider = await requestContext.Server.Upload(
                serviceProvider,
                requestContext.ToOutputFileName(blobResourceContents.MimeType!.ResolveExtensionFromMime()),
                BinaryData.FromBytes(Convert.FromBase64String(blobResourceContents.Blob)),
                cancellationToken);

            return [FileExtensionContentTypeProvider!, metadata];
        }

        return [respone.Content, metadata];
    }
}

