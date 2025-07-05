using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextCompletionsExtensions
{
    public static CompletionsCapability? ToCompletionsCapability(this ServerConfig server,
        CompletionService completionService, Dictionary<string, string>? headers = null)
    {
        var hasComletion = completionService.CanComplete(server);

        return hasComletion ? new CompletionsCapability()
        {
            CompleteHandler = async (request, cancellationToken)
                =>
            {
                request.Services!.WithHeaders(headers);
                return await completionService.GetCompletion(request.Params, server, request.Services!, request.Server, cancellationToken);
            },

        } : null;
    }
}