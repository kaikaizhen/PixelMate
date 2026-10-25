using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PixelMate.Api.Realtime;

/// <summary>Server to client only. A connection joins exactly one group, "u:{its own user id}", chosen from the signed-in cookie, never from anything the client sends.</summary>
[Authorize]
public sealed class PetHub : Hub
{
    public const string Event = "pet.event";
    public static string Group(Guid userId) => $"u:{userId}";

    public override async Task OnConnectedAsync()
    {
        var id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(id, out var userId)) await Groups.AddToGroupAsync(Context.ConnectionId, Group(userId));
        await base.OnConnectedAsync();
    }
}
