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

    [Description("Get Simplicate leaves totals grouped on employee and leave type")]
    [McpServerTool(Name = "SimplicateHRM_GetLeaveTotals", ReadOnly = true, UseStructuredContent = true)]
    public static async Task<Dictionary<string, List<LeaveTotals>>?> SimplicateHRM_GetLeaveTotals(
        [Description("Team to get the leave totals for")] string teamName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Filter on leave type")] string? leaveType = null,
        CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string employeeUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/hrm/employee";
        string employeeSelect = "id";
        var employeeFilters = new List<string>
        {
            "q[employment_status]=active",
            $"q[teams.name]=*{teamName}",
            "q[is_user]=true",
            "q[type.label]=internal",
            $"select={employeeSelect}"
        };

        var employeeFilterString = string.Join("&", employeeFilters);
        var employees = await downloadService.GetAllSimplicatePagesAsync<SimplicateIdItem>(
                  serviceProvider,
                  requestContext.Server,
                  employeeUrl,
                  employeeFilterString,
                  pageNum => $"Downloading employees {teamName} leaves page {pageNum}",
                  requestContext,
                  cancellationToken: cancellationToken
              );

        var selectedId = employees.OfType<SimplicateIdItem>().Select(a => a.Id);

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/hrm/leave";
        string select = "employee.,hours,leavetype.";
        var filters = new List<string>
        {
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
            leaves = [.. leaves.Where(a => a.LeaveType?.Label?.Contains(leaveType, StringComparison.OrdinalIgnoreCase) == true)];
        }

        return leaves
            .Where(a => selectedId.Contains(a.Employee?.Id))
            .GroupBy(x => new { Employee = x.Employee?.Name ?? "" })
            .OrderBy(x => x.Key.Employee)
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

    public class SimplicateIdItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class Employee
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
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

