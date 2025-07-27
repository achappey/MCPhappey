using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Beta;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Export;

public static class SimplicateExport
{
    [Description("Downloads data from Simplicate for the specified resource or endpoint, and generates a downloadable CSV export. Supports filtering and full pagination to include all matching records. The resulting CSV file contains all main fields and nested object values for consistent reporting.")]
    [McpServerTool(Name = "Simplicate_CreateCSVExport", Title = "Create a CSV export from Simplicate data",
        ReadOnly = false, Idempotent = false)]
    public static async Task<CallToolResult?> Simplicate_CreateCSVExport(
        [Description("Server relative url of the Simplicate API enpoint with filters (eg /projects/project or /crm/organizations)")]
        string simplicateUrl,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("CSV delimiter (comma, semicolon, tab, etc). Defaults to comma.")]
        char delimiter = ',',
        CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();

        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app";
        // Simplicate CRM Organization endpoint
        var fullUrl = simplicateUrl?.StartsWith("/api/v2") == true ? $"{baseUrl}{simplicateUrl}"
            : $"{baseUrl}/api/v2{simplicateUrl}";

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        var items = await downloadService.GetAllSimplicatePagesAsync(
            serviceProvider,
            requestContext.Server,
            fullUrl,
            pageNum => $"Downloading projects",
            requestContext,
            cancellationToken: cancellationToken
        );

        var options = new JsonCsvConverter.CsvOptions
        {
            Order = JsonCsvConverter.ColumnOrder.Alphabetical,
            Delimiter = delimiter
        };

        string csv = JsonCsvConverter.ToCsv(JsonSerializer.Serialize(items), options);

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);

        var outputName = $"Simplicate_Export_{DateTime.Now.Ticks}.csv";
        var uploadStream = new MemoryStream(BinaryData.FromString(csv).ToArray());

        var myDrive = await client.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var uploadedItem = await client.Drives[myDrive?.Id].Root.ItemWithPath($"/{outputName}")
            .Content.PutAsync(uploadStream, cancellationToken: cancellationToken);

        return uploadedItem?.WebUrl?.ToTextCallToolResponse();
    }


}

