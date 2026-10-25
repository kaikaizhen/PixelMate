using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Items;

namespace PixelMate.Api.Rooms;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoom(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/room").RequireAuthorization();
        g.MapGet("/", Get);
        g.MapPut("/", Save).Validate<SaveRoomRequest>();
        return api;
    }

    private static Guid UserId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static IResult Err(HttpContext ctx, int status, string code, string message) => Results.Json(new ApiError(code, message, ctx.TraceIdentifier), statusCode: status);

    internal static async Task<RoomDto> Build(AppDbContext db, Guid owner, RoomState state, CancellationToken ct)
    {
        var rows = await db.RoomPlacements.AsNoTracking().Where(p => p.OwnerId == owner).OrderBy(p => p.Y).ThenBy(p => p.X).ThenBy(p => p.PlacementId).ToListAsync(ct);
        return new RoomDto(state.Revision, state.FloorItemId, rows.Select(p => new PlacementDto(p.PlacementId, p.ItemId, p.X, p.Y)).ToList());
    }

    /// <summary>The first read creates the room with the starter table, which the player now owns like anything else (movable, removable).</summary>
    private static async Task<RoomState> StateOrStarter(AppDbContext db, Guid owner, TimeProvider clock, CancellationToken ct)
    {
        var state = await db.RoomStates.AsNoTracking().FirstOrDefaultAsync(s => s.OwnerId == owner, ct);
        if (state is not null) return state;
        if (!await db.ItemDefinitions.AnyAsync(d => d.ItemId == RoomRules.StarterItem, ct))
            return new RoomState { OwnerId = owner };                  // no catalogue (tests without a seed): an empty room
        var now = clock.GetUtcNow().UtcDateTime;
        var fresh = new RoomState { OwnerId = owner, UpdatedAt = now };
        db.RoomStates.Add(fresh);
        db.RoomPlacements.Add(new RoomPlacement { OwnerId = owner, ItemId = RoomRules.StarterItem, X = RoomRules.StarterX, Y = RoomRules.StarterY });
        var row = await db.InventoryEntries.FirstOrDefaultAsync(i => i.OwnerId == owner && i.ItemId == RoomRules.StarterItem, ct);
        if (row is null) db.InventoryEntries.Add(new InventoryEntry { OwnerId = owner, ItemId = RoomRules.StarterItem, Quantity = 1 });
        else if (row.Quantity < 1) row.Quantity = 1;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();                                   // two first reads raced: the other one created it
            return await db.RoomStates.AsNoTracking().FirstAsync(s => s.OwnerId == owner, ct);
        }
        db.ChangeTracker.Clear();
        return fresh;
    }

    private static async Task<IResult> Get(ClaimsPrincipal principal, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var me = UserId(principal);
        var state = await StateOrStarter(db, me, clock, ct);
        return Results.Ok(await Build(db, me, state, ct));
    }

    /// <summary>
    /// Replaces the whole layout at once (the editor sends what the room looks like when the player taps 完成): every item must be an
    /// owned furniture / decor / collectible, no more copies than owned, the floor must be an owned floor tile. One transaction + revision.
    /// </summary>
    private static async Task<IResult> Save(SaveRoomRequest req, ClaimsPrincipal principal, AppDbContext db, Realtime.EventOutbox outbox, TimeProvider clock, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var state = await StateOrStarter(db, me, clock, ct);
        if (state.Revision != req.ExpectedRevision) return Err(ctx, 409, ErrorCodes.RevisionConflict, "小屋已經在別的地方更新了，已幫你重新載入。");

        var ids = req.Placements.Select(p => p.ItemId).Concat(req.FloorItemId is null ? [] : [req.FloorItemId]).Distinct().ToList();
        var defs = await db.ItemDefinitions.AsNoTracking().Where(d => ids.Contains(d.ItemId)).ToDictionaryAsync(d => d.ItemId, ct);
        var owned = await db.InventoryEntries.AsNoTracking().Where(i => i.OwnerId == me && ids.Contains(i.ItemId)).ToDictionaryAsync(i => i.ItemId, i => i.Quantity, ct);

        if (req.FloorItemId is not null)
        {
            if (!RoomRules.IsFloor(req.FloorItemId) || !defs.ContainsKey(req.FloorItemId)) return Err(ctx, 422, ErrorCodes.InvalidState, "這不是地板。");
            if (owned.GetValueOrDefault(req.FloorItemId) < 1) return Err(ctx, 422, ErrorCodes.InvalidState, "背包裡沒有這款地板。");
        }
        foreach (var g in req.Placements.GroupBy(p => p.ItemId))
        {
            if (!defs.TryGetValue(g.Key, out var def) || !RoomRules.PlaceableCategories.Contains(def.Category) || RoomRules.IsFloor(g.Key))
                return Err(ctx, 422, ErrorCodes.InvalidState, "這個道具不能放進小屋。");
            var usable = owned.GetValueOrDefault(g.Key) - (req.FloorItemId == g.Key ? 1 : 0);
            if (g.Count() > usable) return Err(ctx, 422, ErrorCodes.InvalidState, $"「{def.Name}」只有 {Math.Max(usable, 0)} 個可以擺。");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var bumped = await db.RoomStates.Where(s => s.OwnerId == me && s.Revision == req.ExpectedRevision)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Revision, req.ExpectedRevision + 1).SetProperty(x => x.FloorItemId, req.FloorItemId).SetProperty(x => x.UpdatedAt, now), ct);
        if (bumped == 0) return Err(ctx, 409, ErrorCodes.RevisionConflict, "小屋已經在別的地方更新了，已幫你重新載入。");   // another device saved a moment ago
        await db.RoomPlacements.Where(p => p.OwnerId == me).ExecuteDeleteAsync(ct);
        outbox.Add(me, Realtime.EventTypes.RoomUpdated, new { revision = req.ExpectedRevision + 1 });
        db.RoomPlacements.AddRange(req.Placements.Select(p => new RoomPlacement { OwnerId = me, ItemId = p.ItemId, X = p.X, Y = p.Y }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var saved = await db.RoomStates.AsNoTracking().FirstAsync(s => s.OwnerId == me, ct);
        return Results.Ok(await Build(db, me, saved, ct));
    }
}
