using System.ComponentModel;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Hours.Models;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Hours;

public static class SimplicateHours
{
    [Description("Get total registered hours grouped by employee, optionally filtered by date range (max 65 days), project, or employee.")]
    [McpServerTool(Name = "SimplicateHours_GetHourTotalsByEmployee", ReadOnly = true, UseStructuredContent = true)]
    public static async Task<Dictionary<string, SimplicateHourTotals>?> SimplicateHours_GetHourTotalsByEmployee(
    IServiceProvider serviceProvider,
    RequestContext<CallToolRequestParams> requestContext,
    [Description("Start date for filtering (inclusive), format yyyy-MM-dd. Optional. If combined with toDate, max range is 65 days.")] string? fromDate = null,
    [Description("End date for filtering (inclusive), format yyyy-MM-dd. Optional. If combined with fromDate, max range is 65 days.")] string? toDate = null,
    [Description("Approval status label to filter by. Optional.")] ApprovalStatusLabel? approvalStatusLabel = null,
    [Description("Invoiced status label to filter by. Optional.")] InvoiceStatus? invoiceStatus = null,
    [Description("Project name to filter by. Optional.")] string? projectName = null,
    [Description("Employee name to filter by. Optional.")] string? employeeName = null,
    CancellationToken cancellationToken = default) => await GetHourTotalsGroupedBy(
        serviceProvider,
        requestContext,
        x => x.Employee?.Name,
        fromDate, toDate, projectName, employeeName, approvalStatusLabel, invoiceStatus, cancellationToken
);

    [Description("Get total registered hours grouped by hour type, optionally filtered by date range (max 65 days), project, or employee.")]
    [McpServerTool(Name = "SimplicateHours_GetHourTotalsByHourType", ReadOnly = true, UseStructuredContent = true)]
    public static async Task<Dictionary<string, SimplicateHourTotals>?> SimplicateHours_GetHourTotalsByHourType(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Start date for filtering (inclusive), format yyyy-MM-dd. Optional. If combined with toDate, max range is 65 days.")] string? fromDate = null,
        [Description("End date for filtering (inclusive), format yyyy-MM-dd. Optional. If combined with fromDate, max range is 65 days.")] string? toDate = null,
        [Description("Approval status label to filter by. Optional.")] ApprovalStatusLabel? approvalStatusLabel = null,
        [Description("Invoiced status label to filter by. Optional.")] InvoiceStatus? invoiceStatus = null,
        [Description("Project name to filter by. Optional.")] string? projectName = null,
        [Description("Employee name to filter by. Optional.")] string? employeeName = null,
        CancellationToken cancellationToken = default) => await GetHourTotalsGroupedBy(
            serviceProvider,
            requestContext,
            x => x.Type?.Label,
            fromDate, toDate, projectName, employeeName, approvalStatusLabel, invoiceStatus, cancellationToken
    );

    [Description("Get total registered hours grouped by project, optionally filtered by date range (max 65 days), or approval status.")]
    [McpServerTool(Name = "SimplicateHours_GetHourTotalsByProject", ReadOnly = true, UseStructuredContent = true)]
    public static async Task<Dictionary<string, SimplicateHourTotals>?> SimplicateHours_GetHourTotalsByProject(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Start date for filtering (inclusive), format yyyy-MM-dd. Optional. If combined with toDate, max range is 65 days.")] string? fromDate = null,
        [Description("End date for filtering (inclusive), format yyyy-MM-dd. Optional. If combined with fromDate, max range is 65 days.")] string? toDate = null,
        [Description("Approval status label to filter by. Optional.")] ApprovalStatusLabel? approvalStatusLabel = null,
        [Description("Invoiced status label to filter by. Optional.")] InvoiceStatus? invoiceStatus = null,
        [Description("Employee name to filter by. Optional.")] string? employeeName = null,
        CancellationToken cancellationToken = default) => await GetHourTotalsGroupedBy(
            serviceProvider,
            requestContext,
            x => x.Project?.Name,
            fromDate, toDate, null, employeeName, approvalStatusLabel, invoiceStatus, cancellationToken
    );

    private static async Task<Dictionary<string, SimplicateHourTotals>?> GetHourTotalsGroupedBy(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            Func<SimplicateHourItem, string?> groupKeySelector,
            string? fromDate = null,
            string? toDate = null,
            string? projectName = null,
            string? employeeName = null,
            ApprovalStatusLabel? approvalStatusLabel = null,
            InvoiceStatus? invoiceStatus = null,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fromDate) && string.IsNullOrWhiteSpace(toDate)
            && string.IsNullOrWhiteSpace(projectName)
            && !invoiceStatus.HasValue
            && !approvalStatusLabel.HasValue
            && string.IsNullOrWhiteSpace(employeeName))
            throw new ArgumentException("At least one filter (fromDate, toDate, projectName, employeeName, approvalStatusLabel, invoiceStatus) must be provided.");

        if (!string.IsNullOrWhiteSpace(fromDate) && !string.IsNullOrWhiteSpace(toDate))
        {
            if (DateTime.TryParse(fromDate, out var fromDt) && DateTime.TryParse(toDate, out var toDt))
            {
                if ((toDt - fromDt).TotalDays > 65)
                    throw new ArgumentException("The date range cannot exceed 65 days.");
            }
        }

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/hours/hours";
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(fromDate))
            filters.Add($"q[start_date][ge]={Uri.EscapeDataString(fromDate)}");
        if (!string.IsNullOrWhiteSpace(toDate))
            filters.Add($"q[start_date][le]={Uri.EscapeDataString(toDate)}");

        if (!string.IsNullOrWhiteSpace(projectName)) filters.Add($"q[project.name]=*{Uri.EscapeDataString(projectName)}*");
        if (!string.IsNullOrWhiteSpace(employeeName)) filters.Add($"q[employee.name]=*{Uri.EscapeDataString(employeeName)}*");
        if (approvalStatusLabel.HasValue) filters.Add($"q[approvalstatus.label]=*{Uri.EscapeDataString(approvalStatusLabel.Value.ToString())}*");
        if (invoiceStatus.HasValue) filters.Add($"q[invoice_status]=*{Uri.EscapeDataString(invoiceStatus.Value.ToString())}*");

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

        return hours
            .GroupBy(x => groupKeySelector(x) ?? string.Empty)
            .ToDictionary(
                g => g.Key,
                g => new SimplicateHourTotals
                {
                    TotalHours = g.Sum(r => r.Hours),
                    TotalAmount = g.Sum(r => r.Amount).ToAmount()
                });
    }
}

