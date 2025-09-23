using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.OpenAI.CodeInterpreter;

public static class OpenAICodeInterpreter
{
    [Description("Run a prompt with OpenAI Code interpreter tool.")]
    [McpServerTool(Title = "OpenAI Code interpreter", Name = "openai_codeinterpreter_run",
        Destructive = false,
        ReadOnly = true)]
    public static async Task<ContentBlock> OpenAICodeInterpreter_Run(
          [Description("Prompt to execute (code is allowed).")]
            string prompt,
          RequestContext<CallToolRequestParams> requestContext,
          [Description("Target model (e.g. gpt-5 or gpt-5-mini).")]
            string model = "gpt-5",
          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

        var respone = await requestContext.Server.SampleAsync(new CreateMessageRequestParams()
        {
            Metadata = JsonSerializer.SerializeToElement(new Dictionary<string, object>()
                {
                    {"openai", new {
                        code_interpreter = new { type = "auto" }
                     } },
                }),
            Temperature = 1,
            MaxTokens = 8192,
            ModelPreferences = model.ToModelPreferences(),
            Messages = [prompt.ToUserSamplingMessage()]
        }, cancellationToken);

        return respone.Content;
    }
}

