using System.ComponentModel;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Hours;

public static class SimplicateHours
{
    [Description("Get total registered hours grouped by employee, optionally filtered by date range and project.")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<EmbeddedResourceBlock> SimplicateHours_GetHourTotalsByEmployee(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        string? fromDate = null,
        string? toDate = null,
        string? projectName = null,
        string? employeeName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fromDate) && string.IsNullOrWhiteSpace(toDate)
            && string.IsNullOrWhiteSpace(projectName)
            && string.IsNullOrWhiteSpace(employeeName))
            throw new ArgumentException("At least one filter (fromDate, toDate, organizationName, employeeName) must be provided.");

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/hours/hours";
        string select = "employee.,project.,hours,tariff.";
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(fromDate))
            filters.Add($"q[start_date][ge]={Uri.EscapeDataString(fromDate)}");
        if (!string.IsNullOrWhiteSpace(toDate))
            filters.Add($"q[start_date][le]={Uri.EscapeDataString(toDate)}");

        if (!string.IsNullOrWhiteSpace(projectName)) filters.Add($"q[project.name]=*{Uri.EscapeDataString(projectName)}*");
        if (!string.IsNullOrWhiteSpace(employeeName)) filters.Add($"q[employee.name]=*{Uri.EscapeDataString(employeeName)}*");
        //  var filterString = string.Join("&", filters) + $"&select={select}";
        var filterString = string.Join("&", filters);

        var hours = await downloadService.GetAllSimplicatePagesAsync<SimplicateHourItem>(
            serviceProvider,
            requestContext.Server,
            baseUrl,
            filterString,
            pageNum => $"Downloading hours",
            requestContext,
            cancellationToken: cancellationToken
        );

        var grouped = hours
            .GroupBy(x => x.Employee?.Name ?? string.Empty)
            .Select(g => new SimplicateHourTotals
            {
                EmployeeName = g.Key,
                TotalHours = g.Select(r => r.Hours).Sum(),
                TotalAmount = g.Select(r => r.Amount).Sum().ToAmount()
            });

        string url = $"{baseUrl}?{filterString}";

        return grouped.ToJsonContentBlock(url);
    }

    public class SimplicateHourTotals
    {
        public string EmployeeName { get; set; } = string.Empty;
        public double TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SimplicateHourItem
    {
        [JsonPropertyName("employee")]
        public SimplicateEmployee? Employee { get; set; }

        [JsonPropertyName("project")]
        public SimplicateProject? Project { get; set; }

        [JsonPropertyName("tariff")]
        public decimal Tariff { get; set; }

        [JsonPropertyName("hours")]
        public double Hours { get; set; }

        [JsonIgnore] // Don't serialize calculated property by default
        public decimal Amount
        {
            get
            {
                // Defensive: if negative hours/tariff are expected, remove checks below
                var hours = Convert.ToDecimal(Hours); // Safe: double to decimal
                var tariff = Tariff;
                // If you need to check for negative values, add:
                // if (hours < 0 || tariff < 0) return 0m;

                var amount = hours * tariff;

                // If you want to round to 2 decimals for currency (bankers rounding):
                return amount.ToAmount();
            }
        }
    }

    public class SimplicateEmployee
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class SimplicateProject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}

