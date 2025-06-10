using MCPhappey.Common.Extensions;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Tools.Extensions;

public static class HttpExtensions
{
    public static async Task<CallToolResponse?> ToCallToolResponseOrErrorAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
            // Use your extension method to generate the error response
            return errorMessage.ToErrorCallToolResponse();
        }

        // You'd handle the happy path in your main logic, or you could deserialize here as needed
        // This is just the error shortcut method.
        return null;
    }
}
