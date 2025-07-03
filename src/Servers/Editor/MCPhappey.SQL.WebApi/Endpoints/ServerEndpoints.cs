// ServerEndpoints.cs
using MCPhappey.SQL.WebApi.Extensions;
using MCPhappey.SQL.WebApi.Models.Dto;
using MCPhappey.SQL.WebApi.Repositories;
using MCPhappey.SQL.WebApi.Services;

namespace MCPhappey.SQL.WebApi.Endpoints;

public static class ServerEndpoints
{
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder app)
    {
        // GET all servers
        // LIST servers for current user (filtered)
        app.MapGet("/api/servers", async (
            ServerService serverService,
            HttpContext ctx) =>
        {
            var servers = await serverService.GetAllServers(ctx.User);
            return Results.Ok(servers);
        });

        // GET a specific server (auth checked)
        app.MapGet("/api/servers/{mcpId}", async (
            string mcpId,
            ServerService serverService,
            ServerRepository serverRepo,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var server = await serverRepo.GetServer(mcpId);
                return server is not null ? Results.Ok(server) : Results.NotFound();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // CREATE new server (editors only)
        app.MapPost("/api/servers", async (
            Server server,
            ServerService serverService,
            ServerRepository serverRepo,
            HttpContext ctx,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                // ServerService should throw UnauthorizedAccessException if not editor
                var created = await serverRepo.CreateServer(server.ToDb(), cancellationToken);
                return Results.Created($"/api/servers/{created.Id}", created);
            }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // UPDATE server (auth checked)
        app.MapPut("/api/servers/{mcpId}", async (
            string mcpId,
            Server updated,
            ServerService serverService,
             ServerRepository serverRepo,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                await serverRepo.UpdateServer(updated.ToDb());
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // DELETE server (auth checked)
        app.MapDelete("/api/servers/{mcpId}", async (
            string mcpId,
            ServerService serverService,
               ServerRepository serverRepo,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var server = await serverRepo.GetServer(mcpId) ?? throw new KeyNotFoundException();
                await serverRepo.DeleteServer(server.Id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        return app;
    }
}