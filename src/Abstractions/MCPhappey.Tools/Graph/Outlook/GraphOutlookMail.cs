using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Tools.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Outlook;

public static class GraphOutlookMail
{
    [Description("Send an e-mail message through Outlook from the current users' mailbox.")]
    [McpServerTool(Name = "GraphOutlookMail_SendMail", ReadOnly = false, OpenWorld = true)]
    public static async Task<CallToolResult?> GraphOutlookMail_SendMail(
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
     [Description("E-mail addresses of the recipients. Use a comma separated list for multiple recipients.")] string? toRecipients = null,
     [Description("E-mail addresses for CC (carbon copy). Use a comma separated list for multiple recipients.")] string? ccRecipients = null,
     [Description("Subject of the e-mail message.")] string? subject = null,
     [Description("Body of the e-mail message.")] string? body = null,
     [Description("Type of the message body (html or text).")] BodyType? bodyType = null,
     CancellationToken cancellationToken = default)
    {
        var (typed, notAccepted) = await requestContext.Server.TryElicit(
            new GraphSendMail
            {
                ToRecipients = toRecipients ?? string.Empty,
                CcRecipients = ccRecipients,
                Subject = subject,
                Body = body,
                BodyType = bodyType ?? BodyType.Text
            },
            cancellationToken
        );
        
        if (notAccepted != null) return notAccepted;

        Message newMessage = new()
        {
            Subject = typed?.Subject,
            Body = new ItemBody
            {
                ContentType = typed?.BodyType,
                Content = typed?.Body
            },
            ToRecipients = typed?.ToRecipients.Split(",").Select(a => a.ToRecipient()).ToList(),
            CcRecipients = typed?.CcRecipients?.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.ToRecipient())
                .ToList() ?? []
        };

        Microsoft.Graph.Beta.Me.SendMail.SendMailPostRequestBody sendMailPostRequestBody =
            new()
            {
                Message = newMessage,
                SaveToSentItems = true
            };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        await client.Me.SendMail.PostAsync(sendMailPostRequestBody, cancellationToken: cancellationToken);

        return sendMailPostRequestBody.ToJsonContentBlock("https://graph.microsoft.com/beta/me/sendmail").ToCallToolResult();
    }

    [Description("Create a draft e-mail message in the current user's Outlook mailbox.")]
    [McpServerTool(Name = "GraphOutlookMail_CreateDraft", ReadOnly = false, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphOutlookMail_CreateDraft(
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
     [Description("E-mail addresses of the recipients. Use a comma separated list for multiple recipients.")] string? toRecipients = null,
     [Description("E-mail addresses for CC (carbon copy). Use a comma separated list for multiple recipients.")] string? ccRecipients = null,
     [Description("Subject of the draft e-mail message.")] string? subject = null,
     [Description("Body of the draft e-mail message.")] string? body = null,
     [Description("Type of the message body (html or text).")] BodyType? bodyType = null,
     CancellationToken cancellationToken = default)
    {
        var (typed, notAccepted) = await requestContext.Server.TryElicit<GraphCreateMailDraft>(
            new GraphCreateMailDraft
            {
                ToRecipients = toRecipients ?? string.Empty,
                CcRecipients = ccRecipients,
                Subject = subject,
                Body = body,
                BodyType = bodyType ?? BodyType.Text
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;

        var newMessage = new Message
        {
            Subject = typed?.Subject,
            Body = new ItemBody
            {
                ContentType = typed?.BodyType,
                Content = typed?.Body
            },
            ToRecipients = typed?.ToRecipients
                ?.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.ToRecipient())
                .ToList() ?? [],
            CcRecipients = typed?.CcRecipients
                ?.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.ToRecipient())
                .ToList() ?? []
        };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var createdMessage = await client.Me.Messages.PostAsync(newMessage, cancellationToken: cancellationToken);

        return createdMessage.ToJsonContentBlock($"https://graph.microsoft.com/beta/me/messages/{createdMessage?.Id}").ToCallToolResult();
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
        [JsonConverter(typeof(JsonStringEnumConverter))]
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
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Description("Type of the message body (html or text).")]
        public BodyType? BodyType { get; set; }

    }
}