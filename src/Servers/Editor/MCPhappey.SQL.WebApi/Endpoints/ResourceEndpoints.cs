// PromptEndpoints.cs
using System.Web;
using MCPhappey.SQL.WebApi.Extensions;
using MCPhappey.SQL.WebApi.Models.Dto;
using MCPhappey.SQL.WebApi.Repositories;
using MCPhappey.SQL.WebApi.Services;

namespace MCPhappey.SQL.WebApi.Endpoints;

public static class ResourceEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder app)
    {
        // GET all resources for a server
        app.MapGet("/api/servers/{mcpId}/resources", async (
            string mcpId,
            ResourceRepository resourceRepo,
            ServerService serverService,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var resources = await resourceRepo.GetResources(mcpId);
                return Results.Ok(resources);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // GET a resource by id
        app.MapGet("/api/servers/{mcpId}/resources/{id}", async (
            string mcpId,
            string id,
            ResourceRepository resourceRepo,
            ServerService serverService,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var resource = await resourceRepo.GetResource(mcpId, HttpUtility.UrlDecode(id));
                return resource is not null
                    ? Results.Ok(resource)
                    : Results.NotFound();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // CREATE resource
        app.MapPost("/api/servers/{mcpId}/resources", async (
            string mcpId,
            Resource resource,
            ResourceRepository resourceRepo,
            ServerService serverService,
            ServerRepository serverRepository,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var server = await serverRepository.GetServer(mcpId) ?? throw new KeyNotFoundException();

                await resourceRepo.AddServerResource(server.Id, resource.Uri, resource.Name, resource.Description);

                return Results.Created($"/api/servers/{mcpId}/resources/{resource.Uri}", resource);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // UPDATE resource
        app.MapPut("/api/servers/{mcpId}/resources/{id}", async (
            string mcpId,
            int id,
            Resource updated,
            ResourceRepository resourceRepo,
            ServerService serverService,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var ok = await resourceRepo.UpdateResource(updated.ToDb());
                return ok != null ? Results.NoContent() : Results.NotFound();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // DELETE resource
        app.MapDelete("/api/servers/{mcpId}/resources/{id}", async (
            string mcpId,
            string id,
            ResourceRepository resourceRepo,
            ServerService serverService,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var resource = await resourceRepo.GetResource(mcpId, id);

                await resourceRepo.DeleteResource(resource!.Id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        return app;
    }
}
