using System.Net.Mime;
using System.Text.Json;
using A2A.Models;
using A2A.Server;
using A2A.Server.Infrastructure;
using MCPhappey.Agent2Agent.Models;
using MCPhappey.Common.Models;
using Microsoft.Graph.Beta.Models;

namespace MCPhappey.Agent2Agent.Extensions;

public static class ContextExtensions
{

    public static FileItem ToTaskFileItem(this TaskRecord? content, string uri, string? filename = null) => new()
    {
        Contents = BinaryData.FromObjectAsJson(new
        {
            content?.ContextId,
            TaskId = content?.Id,
            content?.Status,
            Artifacts = content?.Artifacts?.Select(r => new
            {
                r.ArtifactId,
                r.Name,
                r.Description,
                r.Metadata
            }),
            content?.Message,
            content?.History,
        }, JsonSerializerOptions.Web),
        MimeType = MediaTypeNames.Application.Json,
        Uri = uri,
        Filename = filename
    };

    public static FileItem ToArtifactFileItem(this Artifact? content, string uri, string? filename = null) => new()
    {
        Contents = BinaryData.FromObjectAsJson(new
        {
            content?.ArtifactId,
            content?.Name,
            content?.Description,
            content?.Metadata,
        }, JsonSerializerOptions.Web),
        MimeType = MediaTypeNames.Application.Json,
        Uri = uri,
        Filename = filename
    };

    public static Agent2AgentViewContext ToViewContext(
        this Agent2AgentContext ctx,
        IDictionary<string, DirectoryObject> usersDict)
        => new()
        {
            ContextId = ctx.ContextId,
            Metadata = ctx.Metadata,
            TaskIds = ctx.TaskIds,
            Owners = [.. ctx.OwnerIds.Select(ownerId =>
            {
                usersDict.TryGetValue(ownerId, out var dirObj);
                var user = dirObj as User;
                return new Agent2AgentUser
                {
                    Id = ownerId,
                    Name = user?.DisplayName ?? ownerId,
                };
            })],
            Users = [.. ctx.UserIds.Select(userId =>
            {
                usersDict.TryGetValue(userId, out var dirObj);
                var user = dirObj as User;
                return new Agent2AgentUser
                {
                    Id = userId,
                    Name = user?.DisplayName ?? userId,
                };
            })]
        };
}
