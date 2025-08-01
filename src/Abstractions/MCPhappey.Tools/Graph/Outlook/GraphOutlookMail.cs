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
    [Description("Set or update the follow-up flag for a mail message in Outlook.")]
    [McpServerTool(Name = "GraphOutlookMail_FlagMail", Title = "Flag mail for follow-up in Outlook", OpenWorld = true)]
    public static async Task<CallToolResult?> GraphOutlookMail_FlagMail(
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
     [Description("ID of the message to flag.")][Required] string messageId,
     [Description("Flag status. Use Flagged, Complete, or NotFlagged. Defaults to Flagged.")]
        FlagStatusEnum? flagStatus = FlagStatusEnum.Flagged,
     [Description("Start date/time for the flag in ISO format (optional).")] string? startDateTime = null,
     [Description("Due date/time for the flag in ISO format (optional).")] string? dueDateTime = null,
     CancellationToken cancellationToken = default)
    {
        var (typed, notAccepted) = await requestContext.Server.TryElicit(
            new GraphFlagMail
            {
                FlagStatus = flagStatus ?? FlagStatusEnum.Flagged,
                StartDateTime = startDateTime != null ? DateTimeOffset.Parse(startDateTime) : null,
                DueDateTime = dueDateTime != null ? DateTimeOffset.Parse(dueDateTime) : null,
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;

        var flag = new FollowupFlag
        {
            FlagStatus = typed?.FlagStatus switch
            {
                FlagStatusEnum.Flagged => FollowupFlagStatus.Flagged,
                FlagStatusEnum.Complete => FollowupFlagStatus.Complete,
                _ => FollowupFlagStatus.NotFlagged
            }
        };

        if (typed?.StartDateTime.HasValue == true)
        {
            flag.StartDateTime = new DateTimeTimeZone
            {
                DateTime = typed.StartDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = typed.StartDateTime.Value.ToDateTimeTimeZone().TimeZone
            };
        }

        if (typed?.DueDateTime.HasValue == true)
        {
            flag.DueDateTime = new DateTimeTimeZone
            {
                DateTime = typed.DueDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = typed.DueDateTime.Value.ToDateTimeTimeZone().TimeZone
            };
        }

        var updatedMessage = new Message
        {
            Flag = flag
        };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        await client.Me.Messages[messageId].PatchAsync(updatedMessage, cancellationToken: cancellationToken);

        return typed.ToJsonContentBlock($"https://graph.microsoft.com/beta/me/messages/{messageId}").ToCallToolResult();
    }

    public enum FlagStatusEnum
    {
        [Description("Not flagged")]
        NotFlagged,
        [Description("Flagged (for follow-up)")]
        Flagged,
        [Description("Complete")]
        Complete
    }

    [Description("Please fill in the mail flagging details")]
    public class GraphFlagMail
    {
        [JsonPropertyName("flagStatus")]
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Description("Flag status. Use Flagged, Complete, or NotFlagged. Defaults to Flagged.")]
        public FlagStatusEnum FlagStatus { get; set; } = FlagStatusEnum.Flagged;

        [JsonPropertyName("startDateTime")]
        [Description("Start date/time for the flag in ISO format (optional).")]
        public DateTimeOffset? StartDateTime { get; set; }

        [JsonPropertyName("dueDateTime")]
        [Description("Due date/time for the flag in ISO format (optional).")]
        public DateTimeOffset? DueDateTime { get; set; }

    }


    [Description("Reply to an e-mail message in Outlook.")]
    [McpServerTool(Name = "GraphOutlookMail_Reply", Title = "Reply to e-mail via Outlook",
       OpenWorld = true)]
    public static async Task<CallToolResult?> GraphOutlookMail_Reply(
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       [Description("ID of the message to reply to.")][Required] string messageId,
       [Description("Reply type: Reply or ReplyAll. Defaults to Reply.")] ReplyTypeEnum? replyType = ReplyTypeEnum.Reply,
       [Description("Content of the reply message.")] string? content = null,
       CancellationToken cancellationToken = default)
    {
        var (typed, notAccepted) = await requestContext.Server.TryElicit(
            new GraphReplyMail
            {
                Comment = content ?? string.Empty,
                ReplyType = replyType ?? ReplyTypeEnum.Reply,
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);

        if (typed?.ReplyType == ReplyTypeEnum.ReplyAll)
        {
            await client.Me.Messages[messageId].ReplyAll.PostAsync(
                new Microsoft.Graph.Beta.Me.Messages.Item.ReplyAll.ReplyAllPostRequestBody { Comment = typed.Comment },
                cancellationToken: cancellationToken);
        }
        else
        {
            await client.Me.Messages[messageId].Reply.PostAsync(
                new Microsoft.Graph.Beta.Me.Messages.Item.Reply.ReplyPostRequestBody { Comment = typed?.Comment },
                cancellationToken: cancellationToken);
        }

        return typed.ToJsonContentBlock($"https://graph.microsoft.com/beta/me/messages/{messageId}/reply")
             .ToCallToolResult();
    }

    public enum ReplyTypeEnum
    {
        Reply,
        ReplyAll
    }

    [Description("Please fill in the reply details")]
    public class GraphReplyMail
    {
        [JsonPropertyName("replyType")]
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Description("Reply type: Reply or ReplyAll. Defaults to Reply.")]
        public ReplyTypeEnum ReplyType { get; set; } = ReplyTypeEnum.Reply;

        [JsonPropertyName("comment")]
        [Required]
        [Description("Reply content")]
        public string Comment { get; set; } = string.Empty;
    }

    [Description("Send an e-mail message through Outlook from the current users' mailbox.")]
    [McpServerTool(Name = "GraphOutlookMail_SendMail", Title = "Send e-mail via Outlook",
        OpenWorld = true)]
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
    [McpServerTool(Name = "GraphOutlookMail_CreateDraft", Title = "Create draft e-mail in Outlook",
        ReadOnly = false, OpenWorld = false)]
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
        var (typed, notAccepted) = await requestContext.Server.TryElicit(
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