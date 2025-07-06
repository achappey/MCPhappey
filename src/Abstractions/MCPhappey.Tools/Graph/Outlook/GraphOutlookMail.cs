using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Outlook;

public static class GraphOutlookMail
{
    [Description("Send an e-mail message through Outlook from the current users' mailbox.")]
    [McpServerTool(Name = "GraphOutlookMail_SendMail", ReadOnly = false)]
    public static async Task<ContentBlock?> GraphOutlookMail_SendMail(
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
            CcRecipients = dto?.CcRecipients
                ?.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(a => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = a.Trim() }
                })
                .ToList()
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

    [Description("Create a draft e-mail message in the current user's Outlook mailbox.")]
    [McpServerTool(Name = "GraphOutlookMail_CreateDraft", ReadOnly = false)]
    public static async Task<ContentBlock?> GraphOutlookMail_CreateDraft(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var dto = await requestContext.Server.GetElicitResponse<GraphCreateMailDraft>(cancellationToken);

        var newMessage = new Message
        {
            Subject = dto?.Subject,
            Body = new ItemBody
            {
                ContentType = dto?.BodyType ?? BodyType.Text,
                Content = dto?.Body
            },
            ToRecipients = dto?.ToRecipients
                ?.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(a => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = a.Trim() }
                })
                .ToList(),
            CcRecipients = dto?.CcRecipients
                ?.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(a => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = a.Trim() }
                })
                .ToList()
        };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var createdMessage = await client.Me.Messages.PostAsync(newMessage, cancellationToken: cancellationToken);

        return createdMessage.ToJsonContentBlock($"https://graph.microsoft.com/beta/me/messages/{createdMessage?.Id}");
    }

    [Description("Please fill in the draft e-mail details")]
    public class GraphCreateMailDraft
    {
        [JsonPropertyName("toRecipients")]
        [Required]
        [Description("E-mail addresses of the recipients. Use a comma separated list for multiple recipients.")]
        public string ToRecipients { get; set; } = string.Empty;

        [JsonPropertyName("ccRecipients")]
        [Description("E-mail addresses for CC (carbon copy). Use a comma separated list for multiple recipients.")]
        public string? CcRecipients { get; set; }

        [JsonPropertyName("subject")]
        [Required]
        [Description("Subject of the draft e-mail message.")]
        public string? Subject { get; set; }

        [JsonPropertyName("body")]
        [Required]
        [Description("Body of the draft e-mail message.")]
        public string? Body { get; set; }

        [JsonPropertyName("bodyType")]
        [Description("Type of the message body (html or text).")]
        public BodyType? BodyType { get; set; }
    }


    [Description("Please fill in the e-mail details")]
    public class GraphSendMail
    {
        [JsonPropertyName("toRecipients")]
        [Required]
        [Description("E-mail addresses of the recipients. Use a comma seperated list for multiple recipients.")]
        public string ToRecipients { get; set; } = string.Empty;

        [JsonPropertyName("ccRecipients")]
        [Description("E-mail addresses for CC (carbon copy). Use a comma separated list for multiple recipients.")]
        public string? CcRecipients { get; set; }

        [JsonPropertyName("subject")]
        [Required]
        [Description("Subject of the e-mail message.")]
        public string? Subject { get; set; }

        [JsonPropertyName("body")]
        [Required]
        [Description("Body of the e-mail message.")]
        public string? Body { get; set; }

        [JsonPropertyName("bodyType")]
        [Description("Type of the message body (html or text).")]
        public BodyType? BodyType { get; set; }

    }
}