using System.ComponentModel;
using System.Text.Json.Serialization;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Projects;

public static class SimplicateProjects
{
    [Description("Get projects grouped by project manager, optionally filtered by date (equal or greater than), project, or employee. At least one filter is required.")]
    [McpServerTool(Name = "SimplicateProjects_GetProjectsByProjectManager", ReadOnly = true, UseStructuredContent = true)]
    public static async Task<Dictionary<string, IEnumerable<SimplicateProject>>?> SimplicateProjects_GetProjectsByProjectManager(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("End date for filtering (on or after), format yyyy-MM-dd. Optional.")] string? date = null,
        [Description("Project manager name to filter by. Optional.")] string? managerName = null,
        [Description("Project status label to filter by. Optional.")] ProjectStatusLabel? projectStatusLabel = null,
        CancellationToken cancellationToken = default) => await GetProjectsGroupedBy(
            serviceProvider,
            requestContext,
            x => x.ProjectManager?.Name,
            managerName, date, projectStatusLabel, cancellationToken);

    private static async Task<Dictionary<string, IEnumerable<SimplicateProject>>> GetProjectsGroupedBy(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            Func<SimplicateProject, string?> groupKeySelector,
            string? managerName = null,
            string? date = null,
            ProjectStatusLabel? projectStatusLabel = null,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(managerName)
            && !projectStatusLabel.HasValue
            && string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("At least one filter (managerName, projectStatusLabel, date) must be provided.");

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/projects/project";
        string select = "project_manager.,name";
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(date))
            filters.Add($"q[end_date][ge]={Uri.EscapeDataString(date)}");

        if (!string.IsNullOrWhiteSpace(managerName)) filters.Add($"q[project_manager.name]=*{Uri.EscapeDataString(managerName)}*");
        if (projectStatusLabel.HasValue) filters.Add($"q[project_status.label]=*{Uri.EscapeDataString(projectStatusLabel.Value.ToString())}*");

        var filterString = string.Join("&", filters) + $"&select={select}";

        var hours = await downloadService.GetAllSimplicatePagesAsync<SimplicateProject>(
            serviceProvider,
            requestContext.Server,
            baseUrl,
            filterString,
            pageNum => $"Downloading projects",
            requestContext,
            cancellationToken: cancellationToken
        );

        return hours
            .GroupBy(x => groupKeySelector(x) ?? string.Empty)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => t)) ?? [];
    }

    public enum ProjectStatusLabel
    {
        active,
        closed
    }

    public class SimplicateProject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("project_manager")]
        public SimplicateProjectManager? ProjectManager { get; set; }

    }

    public class SimplicateProjectManager
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

}

