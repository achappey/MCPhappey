using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextResourceExtensions
{
    public static ResourcesCapability? ToResourcesCapability(this ServerConfig server, string? authToken = null)
        => server.Server.Capabilities != null ?
            new ResourcesCapability()
            {
                ListResourcesHandler = async (request, cancellationToken)
                    =>
                {
                    var service = request.Services!.GetRequiredService<ResourceService>();

                    return await service.GetServerResources(server, authToken, cancellationToken);
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

                    return await scraper.GetServerResource(server, request.Params?.Uri!,
                        authToken, cancellationToken);
                },
            }
            : null;
}