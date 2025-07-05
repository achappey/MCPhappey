using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Common;

//
// Summary:
//     Interface for auto completion
public interface IAutoCompletion
{
    bool SupportsHost(ServerConfig serverConfig);

    Task<CompleteResult?> GetCompletion(IMcpServer mcpServer, IServiceProvider serviceProvider,
        CompleteRequestParams? completeRequestParams, CancellationToken cancellationToken = default);
}