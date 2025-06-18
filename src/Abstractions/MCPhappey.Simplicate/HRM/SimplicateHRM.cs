using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.HRM;

public static class SimplicateHRMService
{
    [Description("Get Simplicate leaves by year grounep on employee and leave type")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<CallToolResponse> SimplicateHRMService_GetLeaveTotals(
        [Description("Year to get the total from")] string year,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        [Description("Filter on leave type")] string? leaveType = null)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        const int PageSize = 100;           // Simplicate hard maximum
        int offset = 0;                     // Current page start
        bool morePages = true;              // Loop guard

        var leaves = new List<(string? Employee, string? TypeLabel, double Hours)>();

        while (morePages)
        {
            // --- Build URL for *this* page -------------------
            var url =
                $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/hrm/leave" +
                $"?q[year]={year}" +
                $"&select=employee.,hours,leavetype." +
                $"&limit={PageSize}&offset={offset}";

            // --- Fetch & parse -------------------------------
            var page = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, url);
            var stringContent = page?.FirstOrDefault()?.Contents.ToString();

            if (string.IsNullOrWhiteSpace(stringContent))
                break; // nothing returned – defensive

            using var doc = JsonDocument.Parse(stringContent);

            var pageLeaves = doc.RootElement
                .GetProperty("data")
                .EnumerateArray()
                .Select(item => (
                    Employee: item.GetProperty("employee").GetProperty("name").GetString(),
                    TypeLabel: item.GetProperty("leavetype").GetProperty("label").GetString(),
                    Hours: item.GetProperty("hours").GetDouble()))
                .ToList();

            leaves.AddRange(pageLeaves);

            // --- Stop when the page wasn't full --------------
            morePages = pageLeaves.Count == PageSize;
            offset += PageSize;
        }

        if (!string.IsNullOrEmpty(leaveType))
        {
            leaves = leaves.Where(a => a.TypeLabel?.Contains(leaveType, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        var grouped = leaves
            .GroupBy(x => new { x.Employee, x.TypeLabel })
            .Select(g => new
            {
                g.Key.Employee,
                g.Key.TypeLabel,
                TotalHours = g.Sum(x => x.Hours)
            });

        return JsonSerializer.Serialize(grouped).ToTextCallToolResponse();
    }
}

