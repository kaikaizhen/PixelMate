using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Pets;

namespace PixelMate.Api.Items;

public static class ItemEndpoints
{
    public static RouteGroupBuilder MapItems(this RouteGroupBuilder api)
    {
        api.MapGet("/inventory", GetInventory).RequireAuthorization();
        api.MapPost("/pets/me/equipment", Equip).RequireAuthorization().Validate<EquipRequest>();
        return api;
    }

    private static Guid UserId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IResult Err(HttpContext ctx, int status, string code, string message) =>
        Results.Json(new ApiError(code, message, ctx.TraceIdentifier), statusCode: status);

    private static async Task<InventoryResponse> BuildInventory(AppDbContext db, Guid owner, CancellationToken ct)
    {
        var worn = await db.PetEquipment.Where(e => db.Pets.Any(p => p.PetId == e.PetId && p.OwnerId == owner)).Select(e => e.InventoryId).ToListAsync(ct);
        var rows = await (from i in db.InventoryEntries
                          join d in db.ItemDefinitions on i.ItemId equals d.ItemId
                          where i.OwnerId == owner && i.Quantity > 0
                          orderby d.Category, d.ItemId
                          select new { i.InventoryId, d.ItemId, d.Name, d.Category, d.Rarity, d.Slot, d.AssetKey, i.Quantity }).ToListAsync(ct);
        return new InventoryResponse(rows.Select(r => new InventoryItemDto(r.InventoryId, r.ItemId, r.Name, r.Category, r.Rarity, r.Slot,
            r.AssetKey, r.Quantity, worn.Contains(r.InventoryId))).ToList());
    }

    private static async Task<IResult> GetInventory(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        Results.Ok(await BuildInventory(db, UserId(principal), ct));

    /// <summary>
    /// Wear (itemId) or take off (itemId = null) one slot. The player must own the item, it must belong to that slot, and a
    /// costume and a hat exclude each other (the costume already includes the head). Pet + equipment change in one transaction.
    /// </summary>
    private static async Task<IResult> Equip(EquipRequest req, ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var owner = UserId(principal);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var pet = await db.Pets.Include(p => p.Equipment).FirstOrDefaultAsync(p => p.OwnerId == owner, ct);
            if (pet is null) return Err(ctx, 404, ErrorCodes.NotFound, "你還沒有小夥伴。");
            if (pet.Revision != req.ExpectedRevision) return Err(ctx, 409, ErrorCodes.RevisionConflict, "小夥伴的資料已經在別的地方更新了，請重新整理後再試。");

            if (req.ItemId is null)
            {
                var current = pet.Equipment.FirstOrDefault(e => e.Slot == req.Slot);
                if (current is not null) pet.Equipment.Remove(current);
            }
            else
            {
                var entry = await (from i in db.InventoryEntries
                                   join d in db.ItemDefinitions on i.ItemId equals d.ItemId
                                   where i.OwnerId == owner && i.ItemId == req.ItemId && i.Quantity > 0
                                   select new { i.InventoryId, d.Slot }).FirstOrDefaultAsync(ct);
                if (entry is null) return Err(ctx, 404, ErrorCodes.NotFound, "背包裡沒有這個道具。");
                if (entry.Slot != req.Slot) return Err(ctx, 422, ErrorCodes.InvalidState, "這個道具不能穿在這個位置。");

                foreach (var clash in pet.Equipment.Where(e => e.Slot == req.Slot || (req.Slot == "costume" && e.Slot == "head") || (req.Slot == "head" && e.Slot == "costume")).ToList())
                {
                    pet.Equipment.Remove(clash);
                }
                pet.Equipment.Add(new PetEquipment { PetId = pet.PetId, Slot = req.Slot, InventoryId = entry.InventoryId, ItemId = req.ItemId });
            }

            pet.Revision++;
            pet.UpdatedAt = DateTime.UtcNow;
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
                if (attempt == 2) return Err(ctx, 409, ErrorCodes.RevisionConflict, "小夥伴的資料已經在別的地方更新了，請重新整理後再試。");
                continue;
            }
            return Results.Ok(new EquipResult(pet.Revision, Slots.Wearable.ToDictionary(s => s, s => pet.Equipment.FirstOrDefault(x => x.Slot == s)?.ItemId)));
        }
        return Err(ctx, 409, ErrorCodes.RevisionConflict, "小夥伴的資料已經在別的地方更新了，請重新整理後再試。");
    }

    /// <summary>Development only: puts items in the bag so wearing can be tried before pets can find anything.</summary>
    internal static RouteGroupBuilder MapItemDev(this RouteGroupBuilder api)
    {
        api.MapPost("/dev/inventory/grant", async (GrantRequest req, ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var owner = UserId(principal);
            if (!await db.ItemDefinitions.AnyAsync(d => d.ItemId == req.ItemId, ct)) return Err(ctx, 404, ErrorCodes.NotFound, "沒有這個道具。");
            var row = await db.InventoryEntries.FirstOrDefaultAsync(i => i.OwnerId == owner && i.ItemId == req.ItemId, ct);
            if (row is null) db.InventoryEntries.Add(new InventoryEntry { OwnerId = owner, ItemId = req.ItemId, Quantity = req.Quantity });
            else row.Quantity += req.Quantity;
            await db.SaveChangesAsync(ct);
            return Results.Ok(await BuildInventory(db, owner, ct));
        }).RequireAuthorization().Validate<GrantRequest>();
        return api;
    }
}
