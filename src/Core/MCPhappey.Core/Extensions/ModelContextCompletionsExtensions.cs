using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextCompletionsExtensions
{
    public static CompletionsCapability? ToCompletionsCapability(this ServerConfig server,
        CompletionService completionService)
    {
        var hasComletion = completionService.CanComplete(server);

        return hasComletion ? new CompletionsCapability()
        {
            CompleteHandler = async (request, cancellationToken)
                => await completionService.GetCompletion(request.Params, server, request.Services!, request.Server, cancellationToken),

        } : null;
    }
}