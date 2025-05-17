using MCPhappey.Auth.Controllers;

namespace MCPhappey.Auth.Extensions;

public static class AuthEndpoints
{
    public static void MapAuth(this WebApplication app)
    {
        app.MapGet("/authorize", AuthorizationController.Handle);
        app.MapPost("/token", TokenController.Handle);
        app.MapGet("/callback", (Delegate)CallbackController.Handle);
        app.MapPost("/register", (Delegate)RegisterController.Handle);
        app.MapGet("/.well-known/jwks.json", JwksController.Handle);
        app.MapGet("/.well-known/oauth-authorization-server", AuthorizationServerMetadataController.Handle);
    }
}
