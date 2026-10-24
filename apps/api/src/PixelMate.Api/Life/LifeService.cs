using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PixelMate.Api.Data;
using PixelMate.Api.Items;
using PixelMate.Api.Pets;

namespace PixelMate.Api.Life;

/// <summary>Makes up a pet's life for finished slots: writes find events and puts the items in the owner's bag, all in one transaction.</summary>
public sealed class LifeService(IOptions<LifeOptions> options)
{
    private readonly LifeOptions _o = options.Value;

    /// <summary>Process every finished slot after the cursor (at most CatchUpDays back). Safe to call from anywhere, any number of times.</summary>
    public async Task<int> CatchUpAsync(AppDbContext db, Guid petId, Guid ownerId, DateTime petCreatedAt, DateTime now, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return await TryCatchUp(db, petId, ownerId, petCreatedAt, now, ct);
            }
            catch (Exception e) when (e is DbUpdateException)   // another writer made up the same slots first (concurrency token or unique key)
            {
                db.ChangeTracker.Clear();
            }
        }
        return 0;
    }

    private async Task<int> TryCatchUp(AppDbContext db, Guid petId, Guid ownerId, DateTime petCreatedAt, DateTime now, CancellationToken ct)
    {
        var last = LifeScheduler.SlotOf(now) - 1;                      // the running slot is not finished yet
        var life = await db.PetLives.FirstOrDefaultAsync(l => l.PetId == petId, ct);
        var cursor = life?.CursorSlot ?? LifeScheduler.SlotOf(petCreatedAt);
        var from = Math.Max(cursor + 1, last - _o.CatchUpDays * LifeScheduler.SlotsPerDay + 1);
        if (from > last)
        {
            if (life is null) { db.PetLives.Add(new PetLife { PetId = petId, CursorSlot = cursor }); await db.SaveChangesAsync(ct); }
            return 0;
        }

        var skills = await db.PetSkills.AsNoTracking().Where(s => s.PetId == petId).ToDictionaryAsync(s => s.SkillKey, s => s.Points, ct);
        var dayStart = LifeScheduler.DayOf(from) * LifeScheduler.SlotsPerDay;
        var known = await db.PetEvents.AsNoTracking().Where(e => e.PetId == petId && e.Slot >= dayStart && e.Slot <= last).Select(e => e.Slot).ToListAsync(ct);
        var perDay = known.GroupBy(LifeScheduler.DayOf).ToDictionary(g => g.Key, g => g.Count());
        var knownSlots = known.ToHashSet();

        List<(string ItemId, string Rarity)>? catalogue = null;
        var granted = new Dictionary<string, int>();
        var added = 0;
        for (var slot = from; slot <= last; slot++)
        {
            if (knownSlots.Contains(slot) || !LifeScheduler.FindsSomething(petId, skills, slot, _o)) continue;
            var day = LifeScheduler.DayOf(slot);
            if (perDay.GetValueOrDefault(day) >= _o.MaxFindsPerDay) continue;
            catalogue ??= (await db.ItemDefinitions.AsNoTracking().Select(d => new { d.ItemId, d.Rarity }).ToListAsync(ct)).Select(d => (d.ItemId, d.Rarity)).ToList();
            if (catalogue.Count == 0) break;
            var item = LifeScheduler.PickItem(petId, skills, slot, catalogue);
            db.PetEvents.Add(new PetEvent { PetId = petId, Slot = slot, ItemId = item, OccurredAt = LifeScheduler.StartOf(slot + 1) });
            perDay[day] = perDay.GetValueOrDefault(day) + 1;
            granted[item] = granted.GetValueOrDefault(item) + 1;
            added++;
        }

        foreach (var (itemId, qty) in granted)
        {
            var row = await db.InventoryEntries.FirstOrDefaultAsync(i => i.OwnerId == ownerId && i.ItemId == itemId, ct);
            if (row is null) db.InventoryEntries.Add(new InventoryEntry { OwnerId = ownerId, ItemId = itemId, Quantity = qty });
            else row.Quantity += qty;
        }

        if (life is null) db.PetLives.Add(new PetLife { PetId = petId, CursorSlot = last });
        else life.CursorSlot = last;                                  // UPDATE ... WHERE cursor = old: a second writer fails here
        await db.SaveChangesAsync(ct);
        return added;
    }
}
