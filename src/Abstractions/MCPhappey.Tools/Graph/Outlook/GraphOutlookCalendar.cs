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
    /// <summary>
    /// Create a new calendar event in the user's Outlook calendar.
    /// </summary>
    [Description("Create a new calendar event in the user's Outlook calendar.")]
    [McpServerTool(Name = "GraphOutlook_CreateCalendarEvent", ReadOnly = false)]
    public static async Task<ContentBlock?> GraphOutlook_CreateCalendarEvent(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        // Prompt user for event details (subject, start, end, etc.)
        var dto = await requestContext.Server.GetElicitResponse<GraphCreateCalendarEvent>(cancellationToken);

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);

        var newEvent = new Event
        {
            Subject = dto.Subject,
            Body = new ItemBody
            {
                ContentType = dto.BodyType ?? BodyType.Text,
                Content = dto.Body
            },
            Start = new DateTimeTimeZone
            {
                DateTime = dto.StartDateTime,
                TimeZone = dto.TimeZone ?? "UTC"
            },
            End = new DateTimeTimeZone
            {
                DateTime = dto.EndDateTime,
                TimeZone = dto.TimeZone ?? "UTC"
            },
            Location = new Location
            {
                DisplayName = dto.Location
            },
            Attendees = string.IsNullOrWhiteSpace(dto.Attendees) ? null :
                dto.Attendees.Split(',')
                    .Select(a => new Attendee
                    {
                        EmailAddress = new EmailAddress { Address = a.Trim() },
                        Type = AttendeeType.Required
                    }).ToList()
        };

        var result = await client.Me.Events.PostAsync(newEvent, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock("https://graph.microsoft.com/beta/me/events");
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