using System.ComponentModel;
using MCPhappey.Common.Extensions;
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

    [Description("Create a new hour registration in Simplicate")]
    [McpServerTool(Title = "Create hour registration", OpenWorld = false)]
    public static async Task<CallToolResult?> SimplicateHours_CreateHourRegistration(
     [Description("The number of hours.")] double hours,
     [Description("The id of the employee.")] string employeeId,
     [Description("The id of the project.")] string projectId,
     [Description("The id of the project service.")] string projectServiceId,
     [Description("The id of the hour type.")] string hourTypeId,
     [Description("The start date of the hour registration.")] DateTime startDate,
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
     CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();

        if (employeeId.StartsWith("employee:") == false)
        {
            return $"Invalid employee id. Expected format: 'employee:[unique_id]'. Use resource https://{simplicateOptions.Organization}.simplicate.app/api/v2/hrm/employee?q[name]=*[nameFilter]* to search by name.".ToErrorCallToolResponse();
        }

        if (projectId.StartsWith("project:") == false)
        {
            return $"Invalid project id. Expected format: 'project:[unique_id]'. Use resource https://{simplicateOptions.Organization}.simplicate.app/api/v2/projects/project?q[name]=*[nameFilter]* to search by name.".ToErrorCallToolResponse();
        }

        if (projectServiceId.StartsWith("service:") == false)
        {
            return $"Invalid projectServiceId id. Expected format: 'service:[unique_id]'. Use resource https://{simplicateOptions.Organization}.simplicate.app/api/v2/projects/service?q[name]=*[nameFilter]* to search by name.".ToErrorCallToolResponse();
        }

        if (hourTypeId.StartsWith("hourstype:") == false)
        {
            return $"Invalid hourTypeId id. Expected format: 'hourstype:[unique_id]'. Use resource https://{simplicateOptions.Organization}.simplicate.app/api/v2/hours/hourstype?q[label]=*[nameFilter]* to search by name.".ToErrorCallToolResponse();
        }

        // Simplicate Hours endpoint
        string baseUrl = simplicateOptions.GetApiUrl("/hours/hours");

        var dto = new SimplicateNewHour
        {
            Hours = hours,
            StartDate = startDate,
            EmployeeId = employeeId,
            ProjectId = projectId,
            TypeId = hourTypeId,
            ProjectServiceId = projectServiceId,
        };

        // Optionally let user confirm/fill fields in Elicit if you want:
        var (dtoItem, notAccepted, result) = await requestContext.Server.TryElicit(dto, cancellationToken);
        if (notAccepted != null) return notAccepted;

        return (await serviceProvider.PostSimplicateItemAsync(
            baseUrl,
            dtoItem!,
            requestContext: requestContext,
            cancellationToken: cancellationToken
        ))?.ToCallToolResult();
    }


    [Description("Get total registered hours grouped by employee, optionally filtered by date range (max 65 days), project, or employee.")]
    [McpServerTool(Title = "Get Simplicate hour totals by employee",
        OpenWorld = false, Destructive = false, ReadOnly = true)]
    public static async Task<CallToolResult> SimplicateHours_GetHourTotalsByEmployee(
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
    [McpServerTool(Title = "Get Simplicate hour totals by hour type",
        OpenWorld = false, Destructive = false, ReadOnly = true)]
    public static async Task<CallToolResult> SimplicateHours_GetHourTotalsByHourType(
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
    [McpServerTool(Title = "Get Simplicate hour totals by project",
        OpenWorld = false, Destructive = false, ReadOnly = true)]
    public static async Task<CallToolResult> SimplicateHours_GetHourTotalsByProject(
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

    private static async Task<CallToolResult> GetHourTotalsGroupedBy(
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
            return "At least one filter (fromDate, toDate, projectName, employeeName, approvalStatusLabel, invoiceStatus) must be provided.".ToErrorCallToolResponse();

        if (!string.IsNullOrWhiteSpace(fromDate) && !string.IsNullOrWhiteSpace(toDate)
            && string.IsNullOrEmpty(employeeName) && string.IsNullOrEmpty(projectName))
        {
            if (DateTime.TryParse(fromDate, out var fromDt) && DateTime.TryParse(toDate, out var toDt))
            {
                if ((toDt - fromDt).TotalDays > 65)
                    return "The date range cannot exceed 65 days without project and/or employee filter.".ToErrorCallToolResponse();
            }
        }

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = simplicateOptions.GetApiUrl("/hours/hours");
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
                }).ToJsonContentBlock($"{baseUrl}?{filterString}").ToCallToolResult();
    }
}

