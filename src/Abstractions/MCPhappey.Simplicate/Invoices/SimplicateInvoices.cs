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
    [McpServerTool(Name = "SimplicateInvoices_GetOpenInvoicesWithDaysOpenByMyOrganization", ReadOnly = true, UseStructuredContent = true)]
    [Description("Returns, per own organization profile, a grouped summary of outstanding debtors: for each customer, shows the total outstanding amount, number of open invoices, and the average number of days invoices have been open (as of today). Perfect for actionable debtor KPI dashboards without hardcoded periods.")]
    public static async Task<Dictionary<string, List<SimplicateOpenInvoiceWithDaysOpen>>?>
        SimplicateInvoices_GetOpenInvoicesWithDaysOpenByMyOrganization(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            [Description("Optional organization name to filter on")] string? organizationName = null,

            CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/invoices/invoice";

        var filters = new List<string>
        {
            "q[status.label][in]=Sended,Expired"
        };

        if (!string.IsNullOrWhiteSpace(organizationName)) filters.Add($"q[organization.name]=*{Uri.EscapeDataString(organizationName)}*");

        var filterString = string.Join("&", filters);
        var invoices = await downloadService.GetAllSimplicatePagesAsync<SimplicateInvoice>(
            serviceProvider,
            requestContext.Server,
            baseUrl,
            filterString,
            pageNum => $"Downloading open invoices",
            requestContext,
            cancellationToken: cancellationToken
        );

        var now = DateTime.UtcNow;

        return invoices
                .GroupBy(x => x.MyOrganizationProfile?.Organization?.Name ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .GroupBy(x => x.Organization?.Name ?? string.Empty)
                        .Select(cg =>
                        {
                            return new SimplicateOpenInvoiceWithDaysOpen
                            {
                                DebtorName = cg.Key,
                                InvoiceCount = cg.Count(),
                                TotalOutstanding = cg.Sum(x => x.TotalOutstanding),
                                AverageDaysOpen = (int?)cg.Average(x => x.DaysOpen),
                            };
                        })
                        .OrderByDescending(x => x.TotalOutstanding)
                        .ToList()
                );
    }

    [McpServerTool(Name = "SimplicateInvoices_GetInvoicesByProjectManager", ReadOnly = true, UseStructuredContent = true)]
    [Description("Returns, per project manager, a list of invoices with invoice number and amount. Ideal for project control and cashflow management.")]
    public static async Task<Dictionary<string, List<SimplicateInvoice>>?>
        SimplicateInvoices_GetExpiredInvoicesByProjectManager(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            [Description("Optional invoice status to filter on")] InvoiceStatusLabel? invoiceStatus = null,
            [Description("Optional organization name to filter on")] string? organizationName = null,
            [Description("Optional project manager name to filter on")] string? projectManagerName = null,

        CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/invoices/invoice";
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(organizationName)) filters.Add($"q[organization.name]=*{Uri.EscapeDataString(organizationName)}*");
        if (invoiceStatus.HasValue) filters.Add($"q[status.label]={invoiceStatus.Value}");

        var filterString = string.Join("&", filters);
        var invoices = await downloadService.GetAllSimplicatePagesAsync<SimplicateInvoice>(
            serviceProvider,
            requestContext.Server,
            baseUrl,
            filterString,
            pageNum => $"Downloading invoices",
            requestContext,
            cancellationToken: cancellationToken
        );

        var now = DateTime.UtcNow;

        return invoices
          .Where(x => x.Projects?.FirstOrDefault()?.ProjectManager?.Name != null)
          .GroupBy(x => x.Projects?.FirstOrDefault()?.ProjectManager?.Name!)
          .Where(a => string.IsNullOrEmpty(projectManagerName) || a.Key.Contains(projectManagerName, StringComparison.OrdinalIgnoreCase))
          .ToDictionary(
              g => g.Key,
              g => g.OrderBy(a => ParseDate(a.Date)).ToList()
          );
    }

    private static DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;
        if (DateTime.TryParse(dateString, out var dt))
            return dt;
        // eventueel: custom parse logic als je een ISO-formaat of andere verwacht
        return null;
    }


    private static int? ParseInt(string? intString)
    {
        if (string.IsNullOrWhiteSpace(intString))
            return null;
        if (int.TryParse(intString, out var dt))
            return dt;
        // eventueel: custom parse logic als je een ISO-formaat of andere verwacht
        return null;
    }

    [McpServerTool(Name = "SimplicateInvoices_GetInvoiceTotalsByMyOrganization", ReadOnly = true, UseStructuredContent = true)]
    [Description("Get total invoices grouped by my organization profile, optionally filtered by date range and organization.")]
    public static async Task<Dictionary<string, SimplicateInvoiceTotals>?> SimplicateInvoices_GetInvoiceTotalsByMyOrganization(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        string? fromDate = null,
        string? toDate = null,
        string? organizationName = null,
        InvoiceStatusLabel? invoiceStatus = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fromDate) && string.IsNullOrWhiteSpace(toDate) && string.IsNullOrWhiteSpace(organizationName)
            && !invoiceStatus.HasValue)
            throw new ArgumentException("At least one filter (fromDate, toDate, organizationName, invoiceStatus) must be provided.");

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/invoices/invoice";
        string select = "total_including_vat,total_excluding_vat,total_outstanding,my_organization_profile.,payment_term.,date";
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(fromDate)) filters.Add($"q[date][ge]={Uri.EscapeDataString(fromDate)}");
        if (!string.IsNullOrWhiteSpace(toDate)) filters.Add($"q[date][le]={Uri.EscapeDataString(toDate)}");
        if (invoiceStatus.HasValue) filters.Add($"q[status.label]={invoiceStatus.Value}");
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


    [McpServerTool(Name = "SimplicateInvoices_GetAveragePaymentTermByMyOrganization", ReadOnly = true, UseStructuredContent = true)]
    [Description("Returns, per my organization profile, a summary of paid invoices: average, minimum, and maximum payment term (days between invoice date and payment date), optionally filtered by date range and organization.")]
    public static async Task<Dictionary<string, PaidInvoicePaymentTermSummary>?> SimplicateInvoices_GetAveragePaymentTermByMyOrganization(
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      string? fromDate = null,
      string? toDate = null,
      string? organizationName = null,
      CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fromDate) && string.IsNullOrWhiteSpace(toDate) && string.IsNullOrWhiteSpace(organizationName))
            throw new ArgumentException("At least one filter (fromDate, toDate, organizationName, invoiceStatus) must be provided.");


        if (string.IsNullOrWhiteSpace(organizationName) && !string.IsNullOrWhiteSpace(fromDate) && !string.IsNullOrWhiteSpace(toDate))
        {
            if (DateTime.TryParse(fromDate, out var fromDt) && DateTime.TryParse(toDate, out var toDt))
            {
                if ((toDt - fromDt).TotalDays > 65)
                    throw new ArgumentException("The date range cannot exceed 65 days.");
            }
        }

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/invoices/invoice";
        string select = "total_including_vat,total_excluding_vat,total_outstanding,my_organization_profile.,date,id";
        var filters = new List<string> { "q[status.label]=Payed" };

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

        var invoiceDetails = new List<(string Org, PaidInvoicePaymentTermDetail Detail)>();
        foreach (var invoice in invoices)
        {
            if (!DateTime.TryParse(invoice.Date, out var invoiceDate))
                continue;

            string paymentsBaseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/invoices/payment";
            string paymentSelect = "date,amount,invoice_id";
            var paymentFilters = new List<string> { $"q[invoice_id]={invoice.Id}" };
            var paymentFilterString = string.Join("&", paymentFilters) + $"&select={paymentSelect}";

            var invoicePayments = await downloadService.GetAllSimplicatePagesAsync<SimplicatePayment>(
                serviceProvider,
                requestContext.Server,
                paymentsBaseUrl,
                paymentFilterString,
                pageNum => $"Downloading payments {invoice.Id}",
                requestContext,
                cancellationToken: cancellationToken
            );

            if (!invoicePayments.Any())
                continue;

            var totalPaid = invoicePayments.Sum(p => p.Amount);

            // Als je wilt checken op volledig betaald: 
            // if (Math.Abs(totalPaid - invoice.TotalIncludingVat) > 0.01m) continue;

            var lastPaymentDate = invoicePayments
                .Select(p => ParseDate(p.Date))
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .OrderByDescending(d => d)
                .FirstOrDefault();

            if (lastPaymentDate == default)
                continue;

            invoiceDetails.Add((
                invoice.MyOrganizationProfile?.Organization?.Name ?? string.Empty,
                new PaidInvoicePaymentTermDetail
                {
                    Id = invoice.Id,
                    Date = invoice.Date,
                    PaymentDate = lastPaymentDate,
                    PaymentTermDays = (lastPaymentDate - invoiceDate).Days,
                    AmountPaid = totalPaid
                }
            ));
        }

        return invoiceDetails
               .GroupBy(x => x.Org)
               .ToDictionary(
                   g => g.Key,
                   g =>
                   {
                       var list = g.Select(x => x.Detail).ToList();
                       return new PaidInvoicePaymentTermSummary
                       {
                           TotalInvoices = list.Count,
                           AveragePaymentTermDays = list.Any() ? list.Average(x => x.PaymentTermDays) : 0,
                           MinPaymentTermDays = list.Any() ? list.Min(x => x.PaymentTermDays) : (int?)null,
                           MaxPaymentTermDays = list.Any() ? list.Max(x => x.PaymentTermDays) : (int?)null,
                       };
                   }
               );
    }

    public class PaidInvoicePaymentTermSummary
    {
        public int TotalInvoices { get; set; }

        public double AveragePaymentTermDays { get; set; }

        public int? MinPaymentTermDays { get; set; }

        public int? MaxPaymentTermDays { get; set; }
    }

    public class PaidInvoicePaymentTermDetail : SimplicateInvoice
    {
        public DateTime PaymentDate { get; set; }
        public int PaymentTermDays { get; set; }
        public decimal AmountPaid { get; set; }
    }


    public class SimplicateOpenInvoiceWithDaysOpen : SimplicateInvoiceTotals
    {
        public string DebtorName { get; set; } = default!;
        public int InvoiceCount { get; set; }
        public int? AverageDaysOpen { get; set; }
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

        [JsonPropertyName("organization")]
        public SimplicateOrganization? Organization { get; set; }

        [JsonPropertyName("payment_term")]
        public SimplicatePaymentTerm? PaymentTerm { get; set; }

        [JsonPropertyName("projects")]
        public IEnumerable<SimplicateProject>? Projects { get; set; }

        [JsonPropertyName("total_including_vat")]
        public decimal TotalIncludingVat { get; set; }

        [JsonPropertyName("total_excluding_vat")]
        public decimal TotalExcludingVat { get; set; }

        [JsonPropertyName("total_outstanding")]
        public decimal TotalOutstanding { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("days_open")]
        public int? DaysOpen
        {
            get
            {
                if (string.IsNullOrEmpty(Date)) return null;
                var now = DateTime.UtcNow;
                var dueDate = ParseDate(Date);
                return dueDate.HasValue ? (int?)(now - dueDate.Value).TotalDays : null;
            }
        }

        [JsonPropertyName("days_overdue")]
        public int? DaysOverdue
        {
            get
            {
                if (string.IsNullOrEmpty(Date)) return null;
                if (string.IsNullOrEmpty(PaymentTerm?.Days)) return null;

                var now = DateTime.UtcNow;
                var fromDate = ParseDate(Date);
                var days = ParseInt(PaymentTerm?.Days);

                if (!fromDate.HasValue || !days.HasValue) return null;
                var dueDate = fromDate.Value.AddDays(days.Value);

                return (int?)(now - dueDate).TotalDays;
            }
        }


        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

    }

    public class SimplicatePayment
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; } = null!;

        [JsonPropertyName("invoice_id")]
        public string InvoiceId { get; set; } = null!;
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

    public class SimplicateProjectManager
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class SimplicateProject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("project_manager")]
        public SimplicateProjectManager? ProjectManager { get; set; }
    }

    public class SimplicatePaymentTerm
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("days")]
        public string Days { get; set; } = string.Empty;

    }

    public enum InvoiceStatusLabel
    {
        Payed,
        Sended,
        Expired,
        Concept
    }
}

