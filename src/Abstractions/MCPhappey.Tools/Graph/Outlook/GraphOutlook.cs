using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Outlook;

public static class GraphOutlook
{
    [Description("Send an e-mail message through Outlook from the current users' mailbox.")]
    [McpServerTool(ReadOnly = false)]
    public static async Task<ContentBlock?> GraphOutlook_SendMail(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var dto = await requestContext.Server.GetElicitResponse<GraphSendMail>(cancellationToken);

        Message newMessage = new()
        {
            Subject = dto?.Subject,
            Body = new ItemBody
            {
                ContentType = dto?.BodyType,
                Content = dto?.Body
            },
            ToRecipients = dto?.ToRecipients.Split(",").Select(a => new Recipient()
            {
                EmailAddress = new EmailAddress()
                {
                    Address = a
                }
            }).ToList(),
        };

        Microsoft.Graph.Beta.Me.SendMail.SendMailPostRequestBody sendMailPostRequestBody =
            new()
            {
                Message = newMessage,
                SaveToSentItems = true
            };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        await client.Me.SendMail.PostAsync(sendMailPostRequestBody, cancellationToken: cancellationToken);

        return sendMailPostRequestBody.ToJsonContentBlock("https://graph.microsoft.com/beta/me/sendmail");
    }


    [Description("Please fill in the e-mail details")]
    public class GraphSendMail
    {
        [JsonPropertyName("toRecipients")]
        [Required]
        [Description("E-mail addresses of the recipients. Use a comma seperated list for multiple recipients.")]
        public string ToRecipients { get; set; } = default!;

        [JsonPropertyName("subject")]
        [Required]
        [Description("Subject of the e-mail message.")]
        public string? Subject { get; set; }

        [JsonPropertyName("body")]
        [Required]
        [Description("Body of the e-mail message.")]
        public string? Body { get; set; }

        [JsonPropertyName("body")]
        [Description("Type of the message body (html or text).")]
        public BodyType? BodyType { get; set; }

    }
}