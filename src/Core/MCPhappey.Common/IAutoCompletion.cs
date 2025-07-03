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

    Task<IEnumerable<FileItem>?> GetCompletion(IMcpServer mcpServer, IServiceProvider serviceProvider,
        CompleteRequestParams completeRequestParams, CancellationToken cancellationToken = default);
}