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

namespace MCPhappey.Simplicate.Invoices;

public static class SimplicateInvoiceService
{
    [McpServerTool(ReadOnly = true)]
    [Description("Get total invoices grouped by my organization profile, optionally filtered by date range and organization.")]
    public static async Task<CallToolResult> SimplicateInvoiceService_GetInvoiceTotalsByMyOrganization(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        string? fromDate = null,
        string? toDate = null,
        string? organizationName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fromDate) && string.IsNullOrWhiteSpace(toDate) && string.IsNullOrWhiteSpace(organizationName))
            throw new ArgumentException("At least one filter (fromDate, toDate, organizationName) must be provided.");

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/invoices/invoice";
        string select = "total_including_vat,total_excluding_vat,total_outstanding,my_organization_profile.";
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(fromDate)) filters.Add($"q[date][ge]={Uri.EscapeDataString(fromDate)}");
        if (!string.IsNullOrWhiteSpace(toDate)) filters.Add($"q[date][le]={Uri.EscapeDataString(toDate)}");
        if (!string.IsNullOrWhiteSpace(organizationName)) filters.Add($"q[organization.name]=*{Uri.EscapeDataString(organizationName)}*");
        var filterString = string.Join("&", filters) + $"&select={select}";

        var invoices = await downloadService.GetAllSimplicatePagesAsync<SimplicateInvoice>(
            serviceProvider,
            requestContext.Server,
            baseUrl,
            filterString,
            pageNum => $"Downloading invoices",
            requestContext,
            cancellationToken: cancellationToken
        );

        var grouped = invoices
            .GroupBy(x => x.MyOrganizationProfile?.Organization?.Name ?? string.Empty)
            .Select(g => new SimplicateInvoiceTotals
            {
                MyOrganizationName = g.Key,
                TotalInvoices = g.Count(),
                TotalIncludingVat = g.Sum(x => x.TotalIncludingVat),
                TotalExcludingVat = g.Sum(x => x.TotalExcludingVat),
                TotalOutstanding = g.Sum(x => x.TotalOutstanding)
            });

        return JsonSerializer.Serialize(grouped).ToTextCallToolResponse();
    }

    public class SimplicateInvoiceTotals
    {
        public string MyOrganizationName { get; set; } = string.Empty;
        public double TotalInvoices { get; set; }
        public double TotalIncludingVat { get; set; }
        public double TotalExcludingVat { get; set; }
        public double TotalOutstanding { get; set; }
    }


    public class SimplicateInvoice
    {
        [JsonPropertyName("my_organization_profile")]
        public MyOrganizationProfile? MyOrganizationProfile { get; set; }

        [JsonPropertyName("total_including_vat")]
        public double TotalIncludingVat { get; set; }

        [JsonPropertyName("total_excluding_vat")]
        public double TotalExcludingVat { get; set; }

        [JsonPropertyName("total_outstanding")]
        public double TotalOutstanding { get; set; }
    }

    public class MyOrganizationProfile
    {
        [JsonPropertyName("organization")]
        public SimplicateOrganization? Organization { get; set; }
    }

    public class SimplicateOrganization
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}

