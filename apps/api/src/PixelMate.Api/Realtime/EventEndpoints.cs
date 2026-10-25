using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Data;

namespace PixelMate.Api.Realtime;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEvents(this RouteGroupBuilder api)
    {
        api.MapHub<PetHub>("/hub").RequireAuthorization();
        api.MapGet("/events", Catchup).RequireAuthorization();
        return api;
    }

    /// <summary>What happened to me after event number <c>after</c> (oldest first). A client that was away uses it to learn what to re-read.</summary>
    internal static async Task<IResult> Catchup(ClaimsPrincipal principal, AppDbContext db, long? after, int? limit, CancellationToken ct)
    {
        var me = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var take = Math.Clamp(limit ?? 100, 1, 200);
        var rows = await db.OutboxEvents.AsNoTracking().Where(e => e.UserId == me && e.Seq > (after ?? 0)).OrderBy(e => e.Seq).Take(take).ToListAsync(ct);
        var latest = await db.OutboxEvents.AsNoTracking().Where(e => e.UserId == me).MaxAsync(e => (long?)e.Seq, ct) ?? 0;
        var events = rows.Select(e => new PetEventMessage(e.Seq, e.EventId, e.Type, e.PetId, e.Revision, e.OccurredAt, JsonDocument.Parse(e.PayloadJson).RootElement.Clone())).ToList();
        return Results.Ok(new { events, latestSeq = latest });
    }
}
