using System.ComponentModel;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Licensing;

public static class GraphLicensing
{
    [Description("Get user SKUs grouped by department. If departmentName is set, only include that department. Users without department are grouped under empty string.")]
    [McpServerTool(Name = "GraphUsers_GetUserSkusPerDepartment", ReadOnly = true, UseStructuredContent = true, OpenWorld = false)]
    public static async Task<Dictionary<string, Dictionary<string, List<string>>>> GraphUsers_GetUserSkusPerDepartment(
        string? departmentName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var skuMap = await BuildSkuMap(client, cancellationToken);
        var result = new Dictionary<string, Dictionary<string, List<string>>>();

        // Only apply department filter if departmentName is provided and non-empty
        string? filter = "userType eq 'Member' and accountEnabled eq true";
        if (!string.IsNullOrEmpty(departmentName))
            filter += $" and department eq '{departmentName.Replace("'", "''")}'"; // SQL-escape any quotes

        var users = await client
            .Users
            .GetAsync(config =>
            {
                config.QueryParameters.Filter = filter;
                config.QueryParameters.Select = ["userPrincipalName", "assignedLicenses", "department"];
                config.QueryParameters.Top = 999;
            }, cancellationToken);

        foreach (var user in users?.Value ?? [])
        {
            var mail = user.UserPrincipalName;
            if (string.IsNullOrWhiteSpace(mail)) continue;

            var dept = user.Department ?? "";

            // Only needed when departmentName is null/empty, otherwise Graph already filtered
            if (string.IsNullOrEmpty(departmentName) && !string.Equals(dept, departmentName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!result.TryGetValue(dept, out var dict))
            {
                dict = [];
                result[dept] = dict;
            }

            var userLicenses = user.AssignedLicenses?
                .Where(l => l.SkuId != null && skuMap.ContainsKey(l.SkuId.ToString()!))
                .Select(l => skuMap[l.SkuId.ToString()!])
                .Distinct()
                .ToList() ?? [];

            if (userLicenses.Count > 0)
                dict[mail] = userLicenses;
        }

        return result;
    }


    private static async Task<Dictionary<string, string>> BuildSkuMap(GraphServiceClient client, CancellationToken cancellationToken)
    {
        var skuMap = new Dictionary<string, string>();
        var skus = await client
            .SubscribedSkus
            .GetAsync(config =>
            {
                config.QueryParameters.Select = ["skuId", "skuPartNumber"];
            }, cancellationToken);

        foreach (var sku in skus?.Value ?? [])
            if (!string.IsNullOrWhiteSpace(sku.SkuPartNumber) && sku.SkuId != null)
                skuMap[sku.SkuId.ToString()!] = sku.SkuPartNumber!;
        return skuMap;
    }

}
