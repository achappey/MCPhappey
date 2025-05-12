namespace MCPhappey.Core.Models;

public class OAuthSettings2
{
    public string TenantId { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public List<string>? Scopes { get; set; }

    public IDictionary<string, ConfidentialClientSettings2>? ConfidentialClients { get; set; }
}

public class ConfidentialClientSettings2
{
    public List<string> ClientSecrets { get; set; } = [];
    public List<string> RedirectUris { get; set; } = [];
}