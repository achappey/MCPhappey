using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;

namespace MCPhappey.Scrapers;

public class StaticTokenAuthProvider(string accessToken) : IAuthenticationProvider
{
    public Task AuthenticateRequestAsync(
         RequestInformation request,
         Dictionary<string, object>? additionalAuthenticationContext = null,
         CancellationToken cancellationToken = default)
    {
        request.Headers["Authorization"] = [$"Bearer {accessToken}"];
        return Task.CompletedTask;
    }
}
