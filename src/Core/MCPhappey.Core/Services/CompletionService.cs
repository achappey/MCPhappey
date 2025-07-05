
using MCPhappey.Common;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Services;

public class CompletionService(
    IEnumerable<IAutoCompletion> autoCompletions)
{
    public bool CanComplete(ServerConfig serverConfig,
        CancellationToken cancellationToken = default) => autoCompletions.Any(z => z.SupportsHost(serverConfig));

    public async Task<CompleteResult> GetCompletion(CompleteRequestParams? completeRequestParams,
         ServerConfig serverConfig,
         IServiceProvider serviceProvider,
         IMcpServer mcpServer,
         CancellationToken cancellationToken = default)
    {
        var bestDecoder = autoCompletions
            .Where(a => a.SupportsHost(serverConfig))
            .FirstOrDefault();

        CompleteResult? fileContent = null;
        if (bestDecoder != null)
        {
            fileContent = await bestDecoder.GetCompletion(mcpServer, serviceProvider, completeRequestParams, cancellationToken);
        }

        return fileContent ?? new CompleteResult();
    }
}
