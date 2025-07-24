using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static MCPhappey.Common.Extensions.ElicitFormExtensions;

namespace MCPhappey.Common.Extensions;

public static class ElicitExtensions
{
    public static async Task<(T? typedResult, CallToolResult? notAccepted)> TryElicit<T>(
     this IMcpServer mcpServer,
     T elicitRequest,
     CancellationToken cancellationToken = default)
     where T : class, new()
    {
        var elicitParams = CreateElicitRequestParamsForType<T>();
        elicitParams.Message = JsonSerializer.Serialize(new ElicitDefaultData<T>()
        {
            Message = elicitParams.Message,
            DefaultValues = elicitRequest
        }, JsonSerializerOptions.Web);

        var result = await mcpServer.GetElicitResponse(elicitRequest, cancellationToken);
        var notAccepted = result?.NotAccepted();
        if (notAccepted != null) return (null, notAccepted);
        var typed = result?.GetTypedResult<T>() ?? throw new Exception("Type cast failed!");
        return (typed, null);
    }

    public static async Task<ElicitResult?> GetElicitResponse<T>(this IMcpServer mcpServer, CancellationToken cancellationToken) where T : new()
    {
        var elicitParams = CreateElicitRequestParamsForType<T>();
        return await mcpServer.ElicitAsync(elicitParams, cancellationToken: cancellationToken);
    }

    public static CallToolResult? NotAccepted(this ElicitResult elicitResult)
        => elicitResult.Action != "accept" ?
        JsonSerializer.Serialize(elicitResult, JsonSerializerOptions.Web).ToErrorCallToolResponse() : null;

    public static async Task<ElicitResult?> GetElicitResponse<T>(this IMcpServer mcpServer, T defaultValues, CancellationToken cancellationToken) where T : new()
    {
        var elicitParams = CreateElicitRequestParamsForType<T>();
        elicitParams.Message = elicitParams.Message.ToElicitDefaultData(defaultValues);

        return await mcpServer.ElicitAsync(elicitParams, cancellationToken: cancellationToken);
    }
}
