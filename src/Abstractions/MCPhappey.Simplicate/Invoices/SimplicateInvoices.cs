using System.ComponentModel;
using System.Text.Json.Serialization;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Invoices;

public static class SimplicateInvoices
{
    [McpServerTool(Name = "SimplicateInvoices_GetInvoiceTotalsByMyOrganization", ReadOnly = true, UseStructuredContent = true)]
    [Description("Get total invoices grouped by my organization profile, optionally filtered by date range and organization.")]
    public static async Task<Dictionary<string, SimplicateInvoiceTotals>?> SimplicateInvoices_GetInvoiceTotalsByMyOrganization(
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

        return invoices
            .GroupBy(x => x.MyOrganizationProfile?.Organization?.Name ?? string.Empty)
            .ToDictionary(g => g.Key, g => new SimplicateInvoiceTotals
            {
                TotalInvoices = g.Count(),
                TotalIncludingVat = g.Sum(x => x.TotalIncludingVat).ToAmount(),
                TotalExcludingVat = g.Sum(x => x.TotalExcludingVat).ToAmount(),
                TotalOutstanding = g.Sum(x => x.TotalOutstanding).ToAmount(),
            });
    }

    public class SimplicateInvoiceTotals
    {
        [JsonPropertyName("totalInvoices")]
        public double TotalInvoices { get; set; }

        [JsonPropertyName("totalIncludingVat")]
        public decimal TotalIncludingVat { get; set; }

        [JsonPropertyName("totalExcludingVat")]
        public decimal TotalExcludingVat { get; set; }

        [JsonPropertyName("totalOutstanding")]
        public decimal TotalOutstanding { get; set; }
    }

    public class SimplicateInvoice
    {
        [JsonPropertyName("my_organization_profile")]
        public MyOrganizationProfile? MyOrganizationProfile { get; set; }

        [JsonPropertyName("total_including_vat")]
        public decimal TotalIncludingVat { get; set; }

        [JsonPropertyName("total_excluding_vat")]
        public decimal TotalExcludingVat { get; set; }

        [JsonPropertyName("total_outstanding")]
        public decimal TotalOutstanding { get; set; }
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

