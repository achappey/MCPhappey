using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextResourceExtensions
{
    public static ResourcesCapability? ToResourcesCapability(this ServerConfig server,
        Dictionary<string, string>? headers = null)
        => server.Server.Capabilities.Resources != null ?
            new ResourcesCapability()
            {
                ListResourcesHandler = async (request, cancellationToken)
                    =>
                {
                    var service = request.Services!.GetRequiredService<ResourceService>();

                    return await service.GetServerResources(server, cancellationToken);
                },
                ListResourceTemplatesHandler = async (request, cancellationToken)
                =>
                {
                    var service = request.Services!.GetRequiredService<ResourceService>();

                    return await service.GetServerResourceTemplates(server, cancellationToken);
                },
                ReadResourceHandler = async (request, cancellationToken) =>
                {
                    var scraper = request.Services!.GetRequiredService<ResourceService>();
                    request.Services!.WithHeaders(headers);

                    try
                    {
                        return await scraper.GetServerResource(request.Services!,
                            request.Server, request.Params?.Uri!,
                            cancellationToken);
                    }
                    catch (Exception e)
                    {
                        var fileMarkdown =
                          $"<details><summary><a href=\"{request.Params?.Uri}\" target=\"blank\">ERROR ReadResource {new Uri(request.Params?.Uri!).Host}</a></summary>\n\n```\n{e.Message}\n```\n</details>";

                        await request.Server.SendMessageNotificationAsync(fileMarkdown, LoggingLevel.Error, CancellationToken.None);

                        return new ReadResourceResult()
                        {
                            Contents = [new TextResourceContents() {
                                MimeType = "text/plain",
                                Text = e.Message,
                                Uri = request.Params?.Uri!
                            }]
                        };
                    }
                },
            }
            : null;
}