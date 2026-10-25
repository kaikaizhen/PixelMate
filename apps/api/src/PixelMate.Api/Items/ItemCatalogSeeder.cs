using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Data;

namespace PixelMate.Api.Items;

/// <summary>
/// Builds item_definitions from assets/manifest.json (copied next to the API as Seed/manifest.json), so the catalogue,
/// the art and the PWA/ESP32 all share the same assetKeys. Idempotent: inserts new items, refreshes name/rarity/slot of known ones.
/// Weather scenes, effects and the tail halves of costumes are not items.
/// </summary>
public static class ItemCatalogSeeder
{
    private static readonly HashSet<string> NotItems = ["window_weather", "effect", "costume_back"];

    public static string RarityFor(string batch, string slot) =>
        slot == "costume" || batch == "B5" ? "precious" : batch is "B3" or "B4" ? "rare" : "common";

    public static string? CategoryFor(string assetKey) => assetKey switch
    {
        var k when k.StartsWith("equip.hat.") => "hat",
        var k when k.StartsWith("equip.accessory.") => "accessory",
        var k when k.StartsWith("equip.feet.") => "shoes",
        var k when k.StartsWith("equip.handheld.") => "handheld",
        var k when k.StartsWith("equip.costume.") => "costume",
        var k when k.StartsWith("world.furniture.") => "furniture",
        var k when k.StartsWith("world.decor.") => "decor",
        var k when k.StartsWith("item.collectible.") => "collectible",
        var k when k.StartsWith("item.stationery.") => "stationery",
        _ => null,
    };

    public static List<ItemDefinition> ReadManifest(Stream manifest)
    {
        using var doc = JsonDocument.Parse(manifest);
        var items = new List<ItemDefinition>();
        foreach (var p in doc.RootElement.GetProperty("assets").EnumerateObject())
        {
            var a = p.Value;
            if (!a.TryGetProperty("designId", out var design) || !a.TryGetProperty("slot", out var slotEl)) continue;   // pet parts, ui art ...
            var slot = slotEl.GetString() ?? "";
            if (NotItems.Contains(slot) || p.Name.EndsWith(".tail")) continue;
            var category = CategoryFor(p.Name);
            if (category is null) continue;
            var batch = a.TryGetProperty("batch", out var b) ? b.GetString() ?? "" : "";
            items.Add(new ItemDefinition
            {
                ItemId = p.Name,
                AssetKey = p.Name,
                Name = a.TryGetProperty("name", out var n) ? n.GetString() ?? p.Name : p.Name,
                Category = category,
                Slot = Slots.Wearable.Contains(slot) ? slot : null,       // room / inventory / mail items are not worn
                Batch = batch,
                DesignId = design.GetString() ?? "",
                Rarity = RarityFor(batch, slot),
            });
        }
        return items;
    }

    public static async Task<int> SeedAsync(AppDbContext db, Stream manifest, CancellationToken ct = default)
    {
        var wanted = ReadManifest(manifest);
        var existing = await db.ItemDefinitions.ToDictionaryAsync(i => i.ItemId, ct);
        var added = 0;
        foreach (var w in wanted)
        {
            if (existing.TryGetValue(w.ItemId, out var cur))
            {
                (cur.Name, cur.Category, cur.Slot, cur.Rarity, cur.Batch, cur.DesignId, cur.AssetKey) = (w.Name, w.Category, w.Slot, w.Rarity, w.Batch, w.DesignId, w.AssetKey);
            }
            else
            {
                db.ItemDefinitions.Add(w);
                added++;
            }
        }
        await db.SaveChangesAsync(ct);
        return added;
    }
}

/// <summary>Runs the seeder once at start-up. A missing table or manifest is logged, never fatal (tests and fresh databases).</summary>
public sealed class ItemSeedHostedService(IServiceScopeFactory scopes, ILogger<ItemSeedHostedService> log, IConfiguration cfg) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (cfg.GetValue("Items:SeedOnStart", true) is false) return;
        var path = Path.Combine(AppContext.BaseDirectory, "Seed", "manifest.json");
        try
        {
            await using var stream = File.OpenRead(path);
            using var scope = scopes.CreateScope();
            var added = await ItemCatalogSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), stream, ct);
            log.LogInformation("Item catalogue ready ({Added} new)", added);
        }
        catch (Exception ex)
        {
            log.LogWarning("Item catalogue not seeded ({ExceptionType})", ex.GetType().Name);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
