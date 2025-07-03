// PromptEndpoints.cs
using MCPhappey.SQL.WebApi.Extensions;
using MCPhappey.SQL.WebApi.Models.Dto;
using MCPhappey.SQL.WebApi.Repositories;
using MCPhappey.SQL.WebApi.Services;

namespace MCPhappey.SQL.WebApi.Endpoints;

public static class PromptEndpoints
{
    public static IEndpointRouteBuilder MapPromptEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/servers/{mcpId}/prompts", async (
            string mcpId, PromptRepository promptRepo, ServerService serverService, HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var prompts = await promptRepo.GetPrompts(mcpId);
                return Results.Ok(prompts.ToDto());
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        app.MapGet("/api/servers/{mcpId}/prompts/{id}", async (
            string mcpId,
            string id,
            PromptRepository promptRepo,
            ServerService serverService,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var prompt = await promptRepo.GetPrompt(mcpId, id);
                return prompt is not null
                    ? Results.Ok(prompt.ToDto())
                    : Results.NotFound();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // CREATE prompt
        app.MapPost("/api/servers/{mcpId}/prompts", async (
            string mcpId,
            Prompt prompt,
            PromptRepository promptRepo,
            ServerService serverService,
            ServerRepository serverRepository,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var server = await serverRepository.GetServer(mcpId)
                    ?? throw new KeyNotFoundException();

                await promptRepo.AddServerPrompt(server.Id, prompt.PromptTemplate,
                    prompt.Name, prompt.Description,
                    prompt.Arguments.Select(a => a.ToDb()));

                return Results.Created($"/api/servers/{mcpId}/prompts/{prompt.Name}", prompt);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // UPDATE prompt
        app.MapPut("/api/servers/{mcpId}/prompts/{id}", async (
            string mcpId,
            int id,
            Prompt updated,
            PromptRepository promptRepo,
            ServerService serverService,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var ok = await promptRepo.UpdatePrompt(updated.ToDb());
                return ok != null ? Results.NoContent() : Results.NotFound();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        // DELETE prompt
        app.MapDelete("/api/servers/{mcpId}/prompts/{id}", async (
            string mcpId,
            string id,
            PromptRepository promptRepo,
            ServerService serverService,
            HttpContext ctx) =>
        {
            try
            {
                await serverService.EnsureUserAuthorizedAsync(mcpId, ctx.User);
                var prompt = await promptRepo.GetPrompt(mcpId, id);

                await promptRepo.DeletePrompt(prompt!.Id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        return app;
    }
}
