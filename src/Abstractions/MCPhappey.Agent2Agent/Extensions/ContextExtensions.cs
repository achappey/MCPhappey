using MCPhappey.Agent2Agent.Models;
using Microsoft.Graph.Beta.Models;

namespace MCPhappey.Agent2Agent.Services;

public static class ContextExtensions
{
    public static Agent2AgentViewContext ToViewContext(
        this Agent2AgentContext ctx,
        IDictionary<string, DirectoryObject> usersDict)
    {
        return new Agent2AgentViewContext
        {
         //   Name = ctx.Metadata.ContainsKey("name") ? ctx.Metadata["name"].ToString()! : string.Empty,
            ContextId = ctx.ContextId,
            Metadata = ctx.Metadata,
            Owners = ctx.OwnerIds.Select(ownerId =>
            {
                usersDict.TryGetValue(ownerId, out var dirObj);
                var user = dirObj as User;
                return new Agent2AgentUser
                {
                    Id = ownerId,
                    Name = user?.DisplayName ?? ownerId,
                };
            }).ToList(),
            Users = ctx.UserIds.Select(userId =>
            {
                usersDict.TryGetValue(userId, out var dirObj);
                var user = dirObj as User;
                return new Agent2AgentUser
                {
                    Id = userId,
                    Name = user?.DisplayName ?? userId,
                };
            }).ToList()
        };
    }
}
