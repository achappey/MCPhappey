using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Deskbird;

public static class DeskbirdPlugin
{
    // --- Hardcoded config (as requested) -------------------------------------------------------------
    private const string BASE_URL = "https://connect.deskbird.com";

    [Description("Cancel an existing Deskbird booking by ID")]
    [McpServerTool(
          Title = "Cancel Deskbird booking",
          OpenWorld = false,
          Destructive = true
      )]
    public static async Task<CallToolResult?> Deskbird_CancelBooking(
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
          [Description("The booking ID (UUID) to cancel")] string bookingId,
          CancellationToken cancellationToken = default)
          => await requestContext.WithExceptionCheck(async () =>
      {
          var settings = serviceProvider.GetService<DeskbirdSettings>();

          // 1) Elicit typed input (prefill with provided argument)
          var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
              new CancelDeskbirdBooking { BookingId = bookingId },
              cancellationToken);

          if (notAccepted != null) return notAccepted;

          if (typed is null || string.IsNullOrWhiteSpace(typed.BookingId))
              return "BookingId is required.".ToErrorCallToolResponse();

          // Optional UUID sanity check (Deskbird uses UUIDs)
          if (!Guid.TryParse(typed.BookingId, out _))
              return "BookingId must be a valid UUID.".ToErrorCallToolResponse();

          // 2) Build HTTP client
          var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
          var http = httpClientFactory.CreateClient();
          http.BaseAddress = new Uri(BASE_URL.TrimEnd('/') + "/");
          http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings?.ApiKey);
          http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

          // 3) PATCH /bookings/{bookingId}/cancel  (no body)
          var req = new HttpRequestMessage(HttpMethod.Patch, $"bookings/{typed.BookingId}/cancel");
          using var res = await http.SendAsync(req, cancellationToken);

          if (!res.IsSuccessStatusCode)
          {
              var body = await res.Content.ReadAsStringAsync(cancellationToken);
              return $"Deskbird cancel error {(int)res.StatusCode} {res.ReasonPhrase}: {body}".ToErrorCallToolResponse();
          }

          // 4) Return a tiny success payload as a content block
          var success = new { bookingId = typed.BookingId, cancelled = true };
          return success.ToJsonContentBlock($"{http.BaseAddress}bookings/{typed.BookingId}/cancel").ToCallToolResult();
      });

    // Typed Elicit DTO
    [Description("Please provide the Deskbird booking ID to cancel.")]
    public class CancelDeskbirdBooking
    {
        [JsonPropertyName("bookingId")]
        [Required]
        [Description("The UUID of the booking to cancel.")]
        public string BookingId { get; set; } = default!;
    }

    [Description("Create a simple deskbird booking (no guest)")]
    [McpServerTool(
        Title = "Create deskbird booking",
        OpenWorld = false,
        Destructive = true
    )]
    public static async Task<CallToolResult?> Deskbird_CreateBooking(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("UUID of the booking user")] string userId,
        [Description("UUID of the resource to book")] string resourceId,
        [Description("Start time (ISO 8601 UTC, e.g. 2025-09-10T08:00:00.000Z)")] DateTime startTime,
        [Description("End time (ISO 8601 UTC, e.g. 2025-09-10T16:00:00.000Z)")] DateTime endTime,
        [Description("Anonymous booking (true/false)")] bool isAnonymousBooking = false,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
    {
        var settings = serviceProvider.GetService<DeskbirdSettings>();

        if (string.IsNullOrWhiteSpace(settings?.ApiKey))
            return "Deskbird API key not configured.".ToErrorCallToolResponse();

        // --- 1) Elicit using a strongly-typed form --------------------------------------------------
        var (typed, notAccepted, elicitRaw) = await requestContext.Server.TryElicit(
            new NewDeskbirdBooking
            {
                UserId = userId,
                ResourceId = resourceId,
                StartTime = startTime,
                EndTime = endTime,
                IsAnonymousBooking = isAnonymousBooking
            },
            cancellationToken);

        if (notAccepted != null) return notAccepted;

        // If typed is null, fall back to raw dictionary (defensive)
        if (typed is null)
            return "Missing booking data.".ToErrorCallToolResponse();

        if (typed.EndTime <= typed.StartTime)
            return "EndTime must be after StartTime.".ToErrorCallToolResponse();

        if (string.IsNullOrWhiteSpace(typed.UserId))
            return "UserId is required.".ToErrorCallToolResponse();

        if (string.IsNullOrWhiteSpace(typed.ResourceId))
            return "ResourceId is required.".ToErrorCallToolResponse();

        // --- 3) POST /bookings -----------------------------------------------------------------------
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var http = httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(BASE_URL.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            userId = typed.UserId,
            startTime = typed.StartTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            endTime = typed.EndTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            isAnonymousBooking = typed.IsAnonymousBooking,
            resourceId = typed.ResourceId
        };

        using var res = await http.PostAsJsonAsync("bookings", payload, cancellationToken);
        var resBody = await res.Content.ReadAsStringAsync(cancellationToken);

        if (!res.IsSuccessStatusCode)
            return $"Deskbird error {(int)res.StatusCode} {res.ReasonPhrase}: {resBody}".ToErrorCallToolResponse();

        var json = JsonSerializer.Deserialize<JsonElement>(resBody);

        return json.ToJsonContentBlock($"{http.BaseAddress}bookings").ToCallToolResult();
    });


    // --- Typed Elicit form --------------------------------------------------------------------------
    [Description("Please fill in the deskbird booking details.")]
    public class NewDeskbirdBooking
    {
        [JsonPropertyName("userId")]
        [Required]
        [Description("The UUID of the booking user.")]
        public string UserId { get; set; } = default!;

        [JsonPropertyName("resourceId")]
        [Required]
        [Description("The UUID of the resource to book.")]
        public string ResourceId { get; set; } = default!;

        [JsonPropertyName("startTime")]
        [Required]
        [Description("Booking start time in ISO 8601 UTC (e.g., 2025-09-10T08:00:00.000Z).")]
        public DateTimeOffset StartTime { get; set; } = default!;

        [JsonPropertyName("endTime")]
        [Required]
        [Description("Booking end time in ISO 8601 UTC (e.g., 2025-09-10T16:00:00.000Z).")]
        public DateTimeOffset EndTime { get; set; } = default!;

        [JsonPropertyName("isAnonymousBooking")]
        [Description("Whether this is an anonymous booking.")]
        public bool IsAnonymousBooking { get; set; } = false;

    }

}
public class DeskbirdSettings
{
    public string ApiKey { get; set; } = default!;
}
