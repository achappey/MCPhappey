using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.OpenAI.OutlookCalendar;

public static class OpenAIOutlookCalendar
{
    private static readonly string[] value =
        ["search_events", "fetch_event", "fetch_events_batch", "list_events", "get_profile"];

    [Description("OpenAI Outlook Calendar connector.")]
    [McpServerTool(Title = "OpenAI Outlook Calendar connector", Name = "openai_outlook_calendar",
        Destructive = false,
        ReadOnly = true)]
    public static async Task<ContentBlock> OpenAI_OutlookCalendar(
          [Description("Prompt to execute.")] string prompt,
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);

        var oboToken = await serviceProvider.GetOboGraphToken(requestContext.Server);
        var respone = await requestContext.Server.SampleAsync(new CreateMessageRequestParams()
        {
            Metadata = JsonSerializer.SerializeToElement(new Dictionary<string, object>()
            {
                {"openai", new {
                    reasoning = new
                            {
                                effort = "medium"
                            },

                    mcp_list_tools = new[] {
                        new {
                            type = "mcp",
                            server_label = "outlook_calendar",
                            authorization = oboToken,
                            connector_id = "connector_outlookcalendar",
                            require_approval = "never",
                            allowed_tools = value,
                        }
                    }
                }},
            }),
            Temperature = 1,
            MaxTokens = 8192,
            ModelPreferences = "gpt-5".ToModelPreferences(),
            Messages = [prompt.ToUserSamplingMessage()]
        }, cancellationToken);

        return respone.Content;
    }
}
