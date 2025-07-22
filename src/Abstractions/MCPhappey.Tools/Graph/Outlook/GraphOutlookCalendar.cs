using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Outlook;

public static class GraphOutlookCalendar
{
    [Description("Create a new calendar event in the user's Outlook calendar.")]
    [McpServerTool(Name = "GraphOutlookCalendar_CreateCalendarEvent", OpenWorld = false)]
    public static async Task<CallToolResult?> GraphOutlookCalendar_CreateCalendarEvent(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Title or subject of the event.")] string? subject = null,
        [Description("Description or body of the event.")] string? body = null,
        [Description("Type of the body content (html or text).")] BodyType? bodyType = null,
        [Description("Start date and time (yyyy-MM-ddTHH:mm:ss format).")] string? startDateTime = null,
        [Description("End date and time (yyyy-MM-ddTHH:mm:ss format).")] string? endDateTime = null,
        [Description("Time zone for the event.")] string? timeZone = null,
        [Description("Location or meeting room.")] string? location = null,
        [Description("E-mail addresses of attendees (comma separated).")] string? attendees = null,
        CancellationToken cancellationToken = default)
    {
        var (typed, notAccepted) = await requestContext.Server.TryElicit<GraphCreateCalendarEvent>(
            new GraphCreateCalendarEvent
            {
                Subject = subject ?? string.Empty,
                Body = body,
                BodyType = bodyType,
                StartDateTime = startDateTime ?? string.Empty,
                EndDateTime = endDateTime ?? string.Empty,
                TimeZone = timeZone,
                Location = location,
                Attendees = attendees
            },
            cancellationToken
        );
        if (notAccepted != null) return notAccepted;

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);

        var newEvent = new Event
        {
            Subject = typed.Subject,
            Body = new ItemBody
            {
                ContentType = typed.BodyType ?? BodyType.Text,
                Content = typed.Body
            },
            Start = new DateTimeTimeZone
            {
                DateTime = typed.StartDateTime,
                TimeZone = typed.TimeZone ?? "UTC"
            },
            End = new DateTimeTimeZone
            {
                DateTime = typed.EndDateTime,
                TimeZone = typed.TimeZone ?? "UTC"
            },
            Location = new Location
            {
                DisplayName = typed.Location
            },
            Attendees = string.IsNullOrWhiteSpace(typed.Attendees) ? null :
                [.. typed.Attendees.Split(',')
                    .Select(a => new Attendee
                    {
                        EmailAddress = new EmailAddress { Address = a.Trim() },
                        Type = AttendeeType.Required
                    })]
        };

        var result = await client.Me.Events.PostAsync(newEvent, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock("https://graph.microsoft.com/beta/me/events").ToCallToolResult();
    }

    /// <summary>
    /// Data for creating a calendar event.
    /// </summary>
    [Description("Fill in the details for the new calendar event.")]
    public class GraphCreateCalendarEvent
    {
        [JsonPropertyName("subject")]
        [Required]
        [Description("Title or subject of the event.")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        [Description("Description or body of the event.")]
        public string? Body { get; set; }

        [JsonPropertyName("bodyType")]
        [Description("Type of the body content (html or text).")]
        public BodyType? BodyType { get; set; }

        [JsonPropertyName("startDateTime")]
        [Required]
        [Description("Start date and time of the event (yyyy-MM-ddTHH:mm:ss format, e.g., 2025-07-05T13:30:00).")]
        public string StartDateTime { get; set; } = string.Empty;

        [JsonPropertyName("endDateTime")]
        [Required]
        [Description("End date and time of the event (yyyy-MM-ddTHH:mm:ss format, e.g., 2025-07-05T14:30:00).")]
        public string EndDateTime { get; set; } = string.Empty;

        [JsonPropertyName("timeZone")]
        [Description("Time zone for the event (e.g., 'W. Europe Standard Time', 'UTC'). Defaults to UTC.")]
        public string? TimeZone { get; set; }

        [JsonPropertyName("location")]
        [Description("Location or meeting room for the event.")]
        public string? Location { get; set; }

        [JsonPropertyName("attendees")]
        [Description("E-mail addresses of attendees. Use a comma separated list for multiple recipients.")]
        public string? Attendees { get; set; }
    }

}