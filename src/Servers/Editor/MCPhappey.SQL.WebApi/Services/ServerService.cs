using System.Security.Claims;
using MCPhappey.SQL.WebApi.Extensions;
using MCPhappey.SQL.WebApi.Repositories;

namespace MCPhappey.SQL.WebApi.Services;

public class ServerService(ServerRepository repo)
{
    private readonly ServerRepository _repo = repo;

    public async Task EnsureUserAuthorizedAsync(string name, ClaimsPrincipal user)
    {
        var server = await _repo.GetServer(name) ?? throw new KeyNotFoundException();

        // Your actual check: e.g., is user in allowed list, has role, etc.
        var userId = user.Identity?.Name;

        // if (!server.AllowedUsers.Contains(userId)) // or however you model it
        //    throw new UnauthorizedAccessException();
    }

    public async Task<IEnumerable<Common.Models.Server>?> GetAllServers(ClaimsPrincipal user)
    {
        var servers = await _repo.GetServers() ?? throw new KeyNotFoundException();

        // Your actual check: e.g., is user in allowed list, has role, etc.
        var userId = user.Identity?.Name;

        return servers.Select(a => a.ToMcpServer());
        // if (!server.AllowedUsers.Contains(userId)) // or however you model it
        //    throw new UnauthorizedAccessException();
    }
}
