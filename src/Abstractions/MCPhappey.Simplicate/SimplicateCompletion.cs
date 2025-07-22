using MCPhappey.Common;
using MCPhappey.Common.Models;
using MCPhappey.Simplicate.Options;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using System.Text.Json.Serialization;

namespace MCPhappey.Simplicate;

public class SimplicateCompletion(
    SimplicateOptions simplicateOptions,
    DownloadService downloadService) : IAutoCompletion
{
    public bool SupportsHost(ServerConfig serverConfig)
        => serverConfig.Server.ServerInfo.Name.StartsWith("Simplicate-");

    public async Task<Completion> GetCompletion(
     IMcpServer mcpServer,
     IServiceProvider serviceProvider,
     CompleteRequestParams? completeRequestParams,
     CancellationToken cancellationToken = default)
    {
        if (completeRequestParams?.Argument?.Name is not string argName || completeRequestParams.Argument.Value is not string argValue)
            return new();

        if (!completionSources.TryGetValue(argName, out var source))
            return new();

        // Use reflection to invoke the generic helper
        var sourceType = source.GetType();
        var tType = sourceType.GenericTypeArguments[0];
        var method = GetType().GetMethod(nameof(CompleteAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.MakeGenericMethod(tType);

        if (method == null)
            return new();

        var urlFactory = sourceType.GetProperty(nameof(CompletionSource<object>.UrlFactory))?.GetValue(source);
        var selector = sourceType.GetProperty(nameof(CompletionSource<object>.Selector))?.GetValue(source);

        if (method == null || urlFactory == null || selector == null)
            return new();

        var objArray = new object[]
        {
                urlFactory,
                selector,
                argValue,
                mcpServer,
                serviceProvider,
                cancellationToken
        };

        var result = method.Invoke(this, objArray);

        if (result is not Task<List<string>> task)
            return new();

        var values = await task;

        return new()
        {
            Values = values
        };

    }

    private async Task<List<string>> CompleteAsync<T>(
        Func<string, string> urlFactory,
        Func<T, string> selector,
        string argValue,
        IMcpServer mcpServer,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var url = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/{urlFactory(argValue)}";
        var items = await downloadService.GetSimplicatePageAsync<T>(serviceProvider, mcpServer, url, cancellationToken);
        return items?.Data?.Take(100).Select(selector).Where(a => !string.IsNullOrEmpty(a)).ToList() ?? [];
    }


    private readonly Dictionary<string, object> completionSources = new()
    {
        ["teamNaam"] = new CompletionSource<SimplicateNameItem>(
            value => $"hrm/team?q[name]=*{value}*&sort=name&select=name",
            item => item.Name),

        ["relatieSoort"] = new CompletionSource<SimplicateLabelItem>(
            value => $"crm/relationtype?q[label]=*{value}*&sort=label&select=label",
            item => item.Label),

        ["salesBron"] = new CompletionSource<SimplicateNameItem>(
            value => $"sales/salessource?q[name]=*{value}*&sort=name&select=name",
            item => item.Name),

        ["salesReden"] = new CompletionSource<SimplicateNameItem>(
            value => $"sales/salesreason?q[name]=*{value}*&sort=name&select=name",
            item => item.Name),

        ["salesVoortgang"] = new CompletionSource<SimplicateLabelItem>(
            value => $"sales/salesprogress?q[label]=*{value}*&sort=label&select=label",
            item => item.Label),

        ["projectNaam"] = new CompletionSource<SimplicateNameItem>(
            value => $"projects/project?q[name]=*{value}*&sort=name&select=name",
            item => item.Name),

        ["projectdienstNaam"] = new CompletionSource<SimplicateNameItem>(
            value => $"projects/service?q[name]=*{value}*&sort=name&select=name",
            item => item.Name),

        ["urenType"] = new CompletionSource<SimplicateLabelItem>(
            value => $"hours/hourstype?q[label]=*{value}*&sort=label&select=label",
            item => item.Label),

        ["medewerkerNaam"] = new CompletionSource<SimplicateNameItem>(
            value => $"hrm/employee?q[name]=*{value}*&sort=name&select=name&q[is_user]=true",
            item => item.Name),

        ["brancheNaam"] = new CompletionSource<SimplicateNameItem>(
            value => $"crm/industry?q[name]=*{value}*&sort=name&select=name",
            item => item.Name),

        ["naamBedrijf"] = new CompletionSource<SimplicateNameItem>(
            value => $"crm/organization?q[name]=*{value}*&sort=name&select=name",
            item => item.Name),

        ["factuurnummer"] = new CompletionSource<SimplicateInvoiceItem>(
            value => $"invoices/invoice?q[invoice_number]={value}*&sort=invoice_number&select=invoice_number",
            item => item.InvoiceNumber),

        ["factuurStatus"] = new CompletionSource<SimplicateNameItem>(
            value => $"invoices/invoicestatus?q[name]=*{value}*&sort=name&select=name",
            item => item.Name.Replace("label_", string.Empty)),

    };

    public class SimplicateDebtorItem
    {
        [JsonPropertyName("organization")]
        public SimplicateNameItem? Organization { get; set; }

        [JsonPropertyName("person")]
        public SimplicateFullNameItem? Person { get; set; } = default!;
    }

    public class SimplicateInvoiceItem
    {
        [JsonPropertyName("invoice_number")]
        public string InvoiceNumber { get; set; } = string.Empty;
    }

    public class SimplicateFullNameItem
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;
    }

    public class SimplicateNameItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class SimplicateLabelItem
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class CompletionSource<T>(Func<string, string> urlFactory, Func<T, string> selector)
    {
        public Func<string, string> UrlFactory { get; set; } = urlFactory;
        public Func<T, string> Selector { get; set; } = selector;
    }
}
