using MCPhappey.Common.Constants;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Servers.SQL.Extensions;

public static class DatabaseExtensions
{
    private static readonly string DEFAULT_SCOPES
        = "https://graph.microsoft.com/User.Read https://graph.microsoft.com/Directory.ReadWrite.All https://graph.microsoft.com/Sites.ReadWrite.All https://graph.microsoft.com/Contacts.Read https://graph.microsoft.com/Bookmark.Read.All https://graph.microsoft.com/Calendars.Read https://graph.microsoft.com/ChannelMessage.Read.All https://graph.microsoft.com/Chat.Read https://graph.microsoft.com/Mail.Read https://graph.microsoft.com/User.ReadWrite.All";

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
                Title = server.Title,
                Version = "1.0.0"
            },
            Groups = server.Secured && server.Groups.Count != 0
                     ? server.Groups.Select(g => g.Id)
                     : null,
            Owners = server.Owners.Count != 0
                     ? server.Owners.Select(o => o.Id)
                     : null,
            OBO = obo,
            Hidden = server.Hidden
        };
    }

    public static Annotations? ToAnnotations(float? prioritoy, bool? assistant, bool? user)
          => assistant.HasValue || prioritoy.HasValue || user.HasValue ? new()
          {
              Priority = prioritoy,
              Audience = assistant == true ? user == true ?
                      [Role.User, Role.Assistant] : [Role.Assistant] :
                      user == true ? [Role.User] : null
          } : null;

    public static Resource ToResource(this Models.Resource resource)
        => new()
        {
            Uri = resource.Uri,
            Name = resource.Name,
            Title = resource.Title,
            Description = resource.Description,
            Annotations = ToAnnotations(resource.Priority, resource.AssistantAudience,
                resource.UserAudience)
        };

    public static ListResourcesResult ToListResourcesResult(this ICollection<Models.Resource> resources)
        => new()
        {
            Resources = [.. resources
                .OrderByDescending(a => a.Priority)
                .Select(a => a.ToResource())]
        };

    public static ResourceTemplate ToResourceTemplate(this Models.ResourceTemplate resource)
        => new()
        {
            UriTemplate = resource.TemplateUri,
            Name = resource.Name,
            Title = resource.Title,
            Description = resource.Description,
            Annotations = ToAnnotations(resource.Priority, resource.AssistantAudience,
                resource.UserAudience)
        };

    public static ListResourceTemplatesResult ToListResourceTemplatesResult(this ICollection<Models.ResourceTemplate> resources)
        => new()
        {
            ResourceTemplates = [.. resources
                .OrderByDescending(a => a.Priority)
                .Select(a => a.ToResourceTemplate())]
        };

    public static PromptArgument ToPromptArgument(this Models.PromptArgument promptArgument)
      => new()
      {
          Name = promptArgument.Name,
          Description = promptArgument.Description,
          Required = promptArgument.Required
      };

    public static Prompt ToPrompt(this Models.Prompt prompt)
       => new()
       {
           Name = prompt.Name,
           Title = prompt.Title,
           Description = prompt.Description,
           Arguments = [.. prompt.Arguments.Select(z => z.ToPromptArgument())]
       };

    public static PromptTemplate ToPromptTemplate(this Models.Prompt prompt)
    => new()
    {
        Prompt = prompt.PromptTemplate,
        Template = prompt.ToPrompt()
    };

    public static PromptTemplates ToPromptTemplates(this ICollection<Models.Prompt> prompts)
        => new()
        {
            Prompts = [.. prompts.Select(a => a.ToPromptTemplate())]
        };
}