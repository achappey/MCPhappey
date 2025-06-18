using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Invoices;

public static class SimplicateInvoiceService
{
    [Description("Get Simplicate invoice totals by status and/or dates")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<CallToolResponse> SimplicateInvoiceService_GetInvoiceTotals(
       IServiceProvider serviceProvider,
      IMcpServer mcpServer,
      //[Description("Invoice status name")] string? status = null,
      [Description("Invoice date on or after (yyyy-mm-dd)")] string? fromDate = null,
      [Description("Invoice date on or before (yyyy-mm-dd)")] string? toDate = null,
      [Description("Invoice organization name (wildcards supported)")] string? organizationName = null
      )
    {
        // --- At least ONE filter must be provided
        if (string.IsNullOrWhiteSpace(fromDate)
            && string.IsNullOrWhiteSpace(toDate) && string.IsNullOrWhiteSpace(organizationName))
        {
            throw new ArgumentException("At least one filter (status, fromDate, toDate, organizationName) must be provided.");
        }

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        const int PageSize = 100;
        int offset = 0;
        bool morePages = true;

        var invoices = new List<(double TotalIncludingVat,
        double TotalExcludingVat, double TotalOutstanding)>();

        while (morePages)
        {
            var filters = new List<string>();

            //  if (!string.IsNullOrWhiteSpace(status))
            //     filters.Add($"q[status.name]={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(fromDate))
                filters.Add($"q[date][ge]={Uri.EscapeDataString(fromDate)}");
            if (!string.IsNullOrWhiteSpace(toDate))
                filters.Add($"q[date][le]={Uri.EscapeDataString(toDate)}");
            if (!string.IsNullOrWhiteSpace(organizationName))
                filters.Add($"q[organization.name]=*{Uri.EscapeDataString(organizationName)}*");

            string filterString = string.Join("&", filters);

            var url = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/invoices/invoice" +
                      $"?{filterString}" +
                      $"&select=total_including_vat,total_excluding_vat,total_outstanding,status.,organization." +
                      $"&limit={PageSize}&offset={offset}";

            var page = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, url);
            var stringContent = page?.FirstOrDefault()?.Contents?.ToString();

            if (string.IsNullOrWhiteSpace(stringContent))
                break;

            using var doc = JsonDocument.Parse(stringContent);

            var pageInvoices = doc.RootElement
                .GetProperty("data")
                .EnumerateArray()
              .Select(item => (
                    //    Status: item.TryGetProperty("status", out var statusProp) && statusProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                    //       Organization: item.TryGetProperty("organization", out var orgProp) && orgProp.TryGetProperty("name", out var orgName) ? orgName.GetString() : null,
                    TotalIncludingVat: item.TryGetProperty("total_including_vat", out var tIncVat) ? tIncVat.GetDouble() : 0.0,
                    TotalExcludingVat: item.TryGetProperty("total_excluding_vat", out var tExcVat) ? tExcVat.GetDouble() : 0.0,
                    TotalOutstanding: item.TryGetProperty("total_outstanding", out var tOutstanding) ? tOutstanding.GetDouble() : 0.0
                ))
                .ToList();

            invoices.AddRange(pageInvoices);

            morePages = pageInvoices.Count == PageSize;
            offset += PageSize;
        }

        // --- Example: group by status (or whatever you want, can change to org, etc)
        /*   var grouped = invoices
               .GroupBy(x => x.Organization ?? "Onbekend")
               .Select(g => new
               {
                   //    Status = g.Key,
                 //  Organization = g.Key,
                   TotalInvoices = g.Count(),
                   TotalIncludingVat = g.Sum(x => x.TotalIncludingVat),
                   TotalExcludingVat = g.Sum(x => x.TotalExcludingVat),
                   TotalOutstanding = g.Sum(x => x.TotalOutstanding)
               });
   */
        var grouped = new
        {
            //    Status = g.Key,
            //  Organization = g.Key,
            TotalInvoices = invoices.Count(),
            TotalIncludingVat = invoices.Sum(x => x.TotalIncludingVat),
            TotalExcludingVat = invoices.Sum(x => x.TotalExcludingVat),
            TotalOutstanding = invoices.Sum(x => x.TotalOutstanding)
        };

        return JsonSerializer.Serialize(grouped).ToTextCallToolResponse();
    }
}

