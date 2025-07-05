using System.ComponentModel;
using System.Text.Json.Serialization;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.HRM;

public static class SimplicateHRM
{

    [Description("Get Simplicate leaves by year grouped on employee and leave type")]
    [McpServerTool(Name = "SimplicateHRM_GetLeaveTotals",ReadOnly = true, UseStructuredContent = true)]
    public static async Task<Dictionary<string, List<LeaveTotals>>?> SimplicateHRM_GetLeaveTotals(
        [Description("Year to get the total from")] string year,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
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
            requestContext.Server,
            baseUrl,
            filterString,
            pageNum => $"Downloading HRM leaves page {pageNum}",
            requestContext, // Geen requestContext want geen progress nodig, of voeg eventueel toe!
            cancellationToken: cancellationToken
        );

        // Extra filter op leaveType (optioneel)
        if (!string.IsNullOrEmpty(leaveType))
        {
            leaves = leaves
                .Where(a => a.LeaveType?.Label?.Contains(leaveType, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        return leaves
            .GroupBy(x => new { Employee = x.Employee?.Name ?? "" })
            .ToDictionary(
                g => g.Key.Employee,
                g => g.GroupBy(x => x.LeaveType?.Label ?? "")
                        .Select(lg => new LeaveTotals
                        {
                            LeaveType = lg.Key,
                            TotalHours = lg.Sum(x => x.Hours)
                        })
                        .ToList()
        );
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

    public class LeaveTotals
    {
        [JsonPropertyName("leavetype")]
        public string LeaveType { get; set; } = string.Empty;

        [JsonPropertyName("totalHours")]
        public double TotalHours { get; set; }
    }
}

