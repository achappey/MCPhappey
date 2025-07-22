using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                    return await service.GetServerResourceTemplates(server);
                },
                ReadResourceHandler = async (request, cancellationToken) =>
                {
                    var scraper = request.Services!.GetRequiredService<ResourceService>();
                    request.Services!.WithHeaders(headers);

                    var logger = request.Services!.GetRequiredService<ILogger<ResourcesCapability>>();
                    logger.LogInformation(
                        "Action={Action} Server={Server} Resource={Resource}",
                        "ReadResource",
                        server.Server.ServerInfo.Name,
                        request.Params?.Uri);

                    return await scraper.GetServerResource(request.Services!,
                        request.Server, request.Params?.Uri!,
                        cancellationToken);
                },
            }
            : null;
}