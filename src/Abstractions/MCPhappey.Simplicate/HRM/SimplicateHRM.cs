using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.HRM;

public static class SimplicateHRMService
{

    [Description("Get Simplicate leaves by year grouped on employee and leave type")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<CallToolResult> SimplicateHRMService_GetLeaveTotals(
        [Description("Year to get the total from")] string year,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        [Description("Filter on leave type")] string? leaveType = null,
        CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/hrm/leave";
        string select = "employee.,hours,leavetype.";
        var filters = new List<string>
        {
            $"q[year]={Uri.EscapeDataString(year)}",
            $"select={select}"
        };

        var filterString = string.Join("&", filters);

        // Typed DTO ophalen via extension method
        var leaves = await downloadService.GetAllSimplicatePagesAsync<SimplicateLeave>(
            serviceProvider,
            mcpServer,
            baseUrl,
            filterString,
            pageNum => $"Downloading HRM leaves",
            // Progressnotification
            null!, // Geen requestContext want geen progress nodig, of voeg eventueel toe!
            cancellationToken: cancellationToken
        );

        // Extra filter op leaveType (optioneel)
        if (!string.IsNullOrEmpty(leaveType))
        {
            leaves = leaves
                .Where(a => a.LeaveType?.Label?.Contains(leaveType, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        var grouped = leaves
            .GroupBy(x => new { x.Employee?.Name, x.LeaveType?.Label })
            .Select(g => new LeaveTotalsResult
            {
                Employee = g.Key.Name ?? string.Empty,
                TypeLabel = g.Key.Label ?? string.Empty,
                TotalHours = g.Sum(x => x.Hours)
            });

        return JsonSerializer.Serialize(grouped).ToTextCallToolResponse();
    }

    // ---------------------- DTOs ------------------------

    public class SimplicateLeave
    {
        [JsonPropertyName("employee")]
        public Employee? Employee { get; set; }

        [JsonPropertyName("leavetype")]
        public LeaveType? LeaveType { get; set; }

        [JsonPropertyName("hours")]
        public double Hours { get; set; }
    }

    public class Employee
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class LeaveType
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class LeaveTotalsResult
    {
        public string Employee { get; set; } = string.Empty;
        public string TypeLabel { get; set; } = string.Empty;
        public double TotalHours { get; set; }
    }
}

