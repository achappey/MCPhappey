using MCPhappey.Common.Constants;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Servers.SQL.Extensions;

public static class DatabaseExtensions
{
    private static readonly string DEFAULT_SCOPES
        = "https://graph.microsoft.com/User.Read https://graph.microsoft.com/Directory.ReadWrite.All https://graph.microsoft.com/Sites.ReadWrite.All https://graph.microsoft.com/Contacts.Read https://graph.microsoft.com/Bookmark.Read.All https://graph.microsoft.com/Calendars.Read https://graph.microsoft.com/ChannelMessage.Read.All https://graph.microsoft.com/Chat.Read https://graph.microsoft.com/Mail.Read";

    public static Server ToMcpServer(this Models.Server server)
    {
        // ----------------------------
        // Build the OBO dictionary
        // ----------------------------
        Dictionary<string, string>? obo = null;

        if (server.Secured)
        {
            obo = [];

            // 1.  Collect distinct hosts that end in ".dynamics.com"
            // 2.  Add them to the dictionary if not already present
            foreach (var host in server.Resources
                                     .Select(r => new Uri(r.Uri).Host)      // or r.Url, adjust to model
                                     .Where(h => h.EndsWith(".dynamics.com", StringComparison.OrdinalIgnoreCase))
                                     .Distinct())
            {
                obo.TryAdd(host, $"https://{host}/.default");
            }

            foreach (var host in server.Resources
                .Select(r =>
                {
                    var uri = new Uri(r.Uri);
                    return (uri.Host, Path: uri.AbsolutePath);
                })
                .Where(x =>
                    !string.IsNullOrEmpty(x.Host)
                    && x.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase)
                    && x.Path?.IndexOf("/_api/", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(x => x.Host)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                obo.TryAdd(host, $"https://{host}/.default");
            }

            if (obo.Count == 0)
            {
                obo.TryAdd(Hosts.MicrosoftGraph, DEFAULT_SCOPES);
            }
        }

        // ----------------------------
        // Return the MCP-flavoured server
        // ----------------------------
        return new Server
        {
            Capabilities = new()
            {
                Prompts = server.Prompts.Count != 0 ? new() : null,
                Resources = server.ResourceTemplates.Count != 0 || server.Resources.Count != 0 ? new() : null,
                Tools = server.Tools.Count != 0 ? new() : null
            },
            Plugins = server.Tools.Select(a => a.Name),
            Instructions = server.Instructions,
            ServerInfo = new()
            {
                Name = server.Name,
                Version = "1.0.0"
            },
            Groups = server.Secured && server.Groups.Count != 0
                     ? server.Groups.Select(g => g.Id)
                     : null,
            Owners = server.Owners.Count != 0
                     ? server.Owners.Select(o => o.Id)
                     : null,
            OBO = obo
        };
    }


    public static ListResourcesResult ToListResourcesResult(this ICollection<Models.Resource> resources)
        => new()
        {
            Resources = [.. resources.Select(a => new Resource()
            {
                Uri = a.Uri,
                Name = a.Name,
                Description = a.Description
            })]
        };

    public static ListResourceTemplatesResult ToListResourceTemplatesResult(this ICollection<Models.ResourceTemplate> resources)
    => new()
    {
        ResourceTemplates = [.. resources.Select(a => new ResourceTemplate()
            {
                UriTemplate = a.TemplateUri,
                Name = a.Name,
                Description = a.Description
            })]
    };

    public static PromptTemplates ToPromptTemplates(this ICollection<Models.Prompt> prompts)
   => new()
   {
       Prompts = [.. prompts.Select(a => new PromptTemplate()
            {
                Prompt = a.PromptTemplate,
                Template = new() {
                    Name = a.Name,
                    Description = a.Description,
                    Arguments = [.. a.Arguments.Select(z => new PromptArgument() {
                        Name = z.Name,
                        Description = z.Description,
                        Required = z.Required
                    })]
                }
            })]
   };
}