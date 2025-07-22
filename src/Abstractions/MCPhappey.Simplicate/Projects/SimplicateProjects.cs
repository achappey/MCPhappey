using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Projects;

public static class SimplicateProjects
{
    [Description("Create a new project in Simplicate")]
    [McpServerTool(Name = "SimplicateProjects_CreateProject", ReadOnly = false, Idempotent = false)]
    public static async Task<CallToolResult?> SimplicateProjects_CreateProject(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();

        // Simplicate CRM Organization endpoint
        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/projects/project";
        var dto = await requestContext.Server.GetElicitResponse<SimplicateNewProject>(cancellationToken);

        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        // Use your POST extension to create the org
        return (await serviceProvider.PostSimplicateItemAsync(
            baseUrl,
            dto!,
            requestContext: requestContext,
            cancellationToken: cancellationToken
        ))?.ToCallToolResult();
    }

    [Description("Get projects grouped by project manager filtered by my organization profile, optionally filtered by date (equal or greater than), project.")]
    [McpServerTool(Name = "SimplicateProjects_GetProjectNamesByProjectManager", ReadOnly = true, UseStructuredContent = true)]
    public static async Task<Dictionary<string, IEnumerable<string>>?> SimplicateProjects_GetProjectNamesByProjectManager(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("My organization profile name of the project filter. Optional.")] string myOrganizationProfileName,
        [Description("End date for filtering (on or after), format yyyy-MM-dd. Optional.")] string? date = null,
        //  [Description("Project manager name to filter by. Optional.")] string? managerName = null,
        [Description("Project status label to filter by. Optional.")] ProjectStatusLabel? projectStatusLabel = null,
        CancellationToken cancellationToken = default) => await GetProjectsGroupedBy(
            serviceProvider,
            requestContext,
            x => x.ProjectManager?.Name,
            myOrganizationProfileName, date, projectStatusLabel, cancellationToken);

    private static async Task<Dictionary<string, IEnumerable<string>>> GetProjectsGroupedBy(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            Func<SimplicateProject, string?> groupKeySelector,
            //  string? managerName = null,
            string? myOrganizationProfileName = null,
            string? date = null,
            ProjectStatusLabel? projectStatusLabel = null,
            CancellationToken cancellationToken = default)
    {
        if (
             //string.IsNullOrWhiteSpace(managerName)
             //&&
             string.IsNullOrWhiteSpace(myOrganizationProfileName)
            //   && !projectStatusLabel.HasValue
            && string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("At least one filter (managerName, date, myOrganizationProfileName) must be provided.");

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/projects/project";
        string select = "project_manager.,name";
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(date))
            filters.Add($"q[end_date][ge]={Uri.EscapeDataString(date)}");

        //  if (!string.IsNullOrWhiteSpace(managerName)) filters.Add($"q[project_manager.name]=*{Uri.EscapeDataString(managerName)}*");
        if (!string.IsNullOrWhiteSpace(myOrganizationProfileName)) filters.Add($"q[my_organization_profile.organization.name]=*{Uri.EscapeDataString(myOrganizationProfileName)}*");
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
                g => g.Select(t => t.Name)) ?? [];
    }

    [Description("Please fill in the project details")]
    public class SimplicateNewProject
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the project.")]
        public string? Name { get; set; }
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
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SimplicateProjectManager? ProjectManager { get; set; }

    }

    public class SimplicateProjectManager
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

}

