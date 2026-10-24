using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Items;
using PixelMate.Api.Pets;

namespace PixelMate.Api.Tests;

public class ItemTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task<(HttpClient C, PetSnapshot Pet, Guid Owner)> Player(AuthFactory f, string email = "a@example.com")
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email);
        await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" });
        var pet = (await (await c.PostAsJsonAsync("/api/v1/pets", new { name = "小栗", bodyVariant = "star_01_cream" })).Content.ReadFromJsonAsync<PetSnapshot>(Web))!;
        using var scope = f.Services.CreateScope();
        var owner = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Pets.AsNoTracking().FirstAsync(p => p.PetId == pet.PetId)).OwnerId;
        return (c, pet, owner);
    }

    private static async Task Give(AuthFactory f, Guid owner, string itemId, int qty = 1)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.InventoryEntries.FirstOrDefaultAsync(i => i.OwnerId == owner && i.ItemId == itemId);
        if (row is null) db.InventoryEntries.Add(new InventoryEntry { OwnerId = owner, ItemId = itemId, Quantity = qty });
        else row.Quantity += qty;
        await db.SaveChangesAsync();
    }

    private static Task<HttpResponseMessage> Equip(HttpClient c, string slot, string? item, int rev) =>
        c.PostAsJsonAsync("/api/v1/pets/me/equipment", new { slot, itemId = item, expectedRevision = rev });

    private static async Task<ApiError> Err(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<ApiError>(Web))!;

    // ---- catalogue ---------------------------------------------------------------------------------------------------
    [Fact]
    public async Task The_catalogue_comes_from_the_manifest_with_the_agreed_rarity_rules()
    {
        using var f = new AuthFactory();
        f.NewClient();
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var all = await db.ItemDefinitions.AsNoTracking().ToListAsync();

        Assert.True(all.Count > 400);
        Assert.Equal(all.Count, all.Select(i => i.ItemId).Distinct().Count());
        Assert.All(all, i => Assert.Equal(i.ItemId, i.AssetKey));
        Assert.DoesNotContain(all, i => i.ItemId.StartsWith("world.weather.") || i.ItemId.StartsWith("fx.") || i.ItemId.EndsWith(".tail"));
        Assert.DoesNotContain(all, i => i.ItemId.StartsWith("pet.star_01"));

        Assert.All(all.Where(i => i.Batch is "B1" or "B2" && i.Category != "costume"), i => Assert.Equal("common", i.Rarity));
        Assert.All(all.Where(i => i.Batch is "B3" or "B4"), i => Assert.Equal("rare", i.Rarity));
        Assert.All(all.Where(i => i.Batch == "B5" || i.Category == "costume"), i => Assert.Equal("precious", i.Rarity));
        Assert.All(all.Where(i => i.Slot is not null), i => Assert.Contains(i.Slot, Slots.Wearable));
        Assert.All(all.Where(i => i.Category is "furniture" or "decor" or "collectible" or "stationery"), i => Assert.Null(i.Slot));
        Assert.Contains(all, i => i.ItemId == "equip.costume.dog" && i.Slot == "costume" && i.Rarity == "precious");
    }

    [Fact]
    public async Task Seeding_twice_adds_nothing_new()
    {
        using var f = new AuthFactory();
        f.NewClient();
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        using var manifest = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Seed", "manifest.json"));
        Assert.Equal(0, await ItemCatalogSeeder.SeedAsync(db, manifest));
    }

    // ---- inventory ---------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Inventory_needs_a_session_and_starts_empty()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/inventory")).StatusCode);
        var (p, _, _) = await Player(f);
        var inv = (await p.GetFromJsonAsync<InventoryResponse>("/api/v1/inventory", Web))!;
        Assert.Empty(inv.Items);
    }

    [Fact]
    public async Task Inventory_lists_what_the_player_owns_with_the_catalogue_details_and_nobody_elses()
    {
        using var f = new AuthFactory();
        var (a, _, aOwner) = await Player(f, "a@example.com");
        var (b, _, bOwner) = await Player(f, "b@example.com");
        await Give(f, aOwner, "equip.hat.straw_post", 2);
        await Give(f, bOwner, "world.furniture.bed_garden");

        var inv = (await a.GetFromJsonAsync<InventoryResponse>("/api/v1/inventory", Web))!;
        var hat = Assert.Single(inv.Items);
        Assert.Equal(("equip.hat.straw_post", "hat", "common", "head", 2, false), (hat.ItemId, hat.Category, hat.Rarity, hat.Slot, hat.Quantity, hat.Equipped));
        Assert.False(string.IsNullOrEmpty(hat.Name));
        var other = (await b.GetFromJsonAsync<InventoryResponse>("/api/v1/inventory", Web))!;
        Assert.Equal("equip.furniture.bed_garden".Replace("equip.", "world."), Assert.Single(other.Items).ItemId);
        Assert.Null(other.Items[0].Slot);
    }

    // ---- wearing -----------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Wearing_an_owned_item_shows_in_the_snapshot_and_bumps_the_revision()
    {
        using var f = new AuthFactory();
        var (c, pet, owner) = await Player(f);
        await Give(f, owner, "equip.hat.straw_post");

        var res = await Equip(c, "head", "equip.hat.straw_post", pet.Revision);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var r = (await res.Content.ReadFromJsonAsync<EquipResult>(Web))!;
        Assert.Equal(pet.Revision + 1, r.Revision);
        Assert.Equal("equip.hat.straw_post", r.Equipment["head"]);
        Assert.Null(r.Equipment["feet"]);

        var snap = (await c.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!;
        Assert.Equal("equip.hat.straw_post", snap.Equipment["head"]);
        var inv = (await c.GetFromJsonAsync<InventoryResponse>("/api/v1/inventory", Web))!;
        Assert.True(inv.Items.Single().Equipped);
    }

    [Fact]
    public async Task Taking_it_off_with_null_clears_the_slot()
    {
        using var f = new AuthFactory();
        var (c, pet, owner) = await Player(f);
        await Give(f, owner, "equip.hat.straw_post");
        var worn = (await (await Equip(c, "head", "equip.hat.straw_post", pet.Revision)).Content.ReadFromJsonAsync<EquipResult>(Web))!;
        var off = await Equip(c, "head", null, worn.Revision);
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.Null((await off.Content.ReadFromJsonAsync<EquipResult>(Web))!.Equipment["head"]);
        Assert.False((await c.GetFromJsonAsync<InventoryResponse>("/api/v1/inventory", Web))!.Items.Single().Equipped);
    }

    [Fact]
    public async Task You_cannot_wear_what_you_do_not_own_or_put_it_in_the_wrong_slot()
    {
        using var f = new AuthFactory();
        var (c, pet, owner) = await Player(f);
        var notOwned = await Equip(c, "head", "equip.hat.straw_post", pet.Revision);
        Assert.Equal(HttpStatusCode.NotFound, notOwned.StatusCode);

        await Give(f, owner, "equip.hat.straw_post");
        var wrongSlot = await Equip(c, "feet", "equip.hat.straw_post", pet.Revision);
        Assert.Equal((HttpStatusCode)422, wrongSlot.StatusCode);
        Assert.Equal(ErrorCodes.InvalidState, (await Err(wrongSlot)).Code);

        await Give(f, owner, "world.furniture.bed_garden");                    // furniture has no slot: it can never be worn
        Assert.Equal((HttpStatusCode)422, (await Equip(c, "head", "world.furniture.bed_garden", pet.Revision)).StatusCode);

        var snap = (await c.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!;
        Assert.All(snap.Equipment.Values, v => Assert.Null(v));
        Assert.Equal(pet.Revision, snap.Revision);                              // nothing changed, nothing bumped
    }

    [Fact]
    public async Task A_new_item_replaces_the_old_one_in_the_same_slot()
    {
        using var f = new AuthFactory();
        var (c, pet, owner) = await Player(f);
        await Give(f, owner, "equip.hat.straw_post");
        await Give(f, owner, "equip.hat.knit_post");
        var first = (await (await Equip(c, "head", "equip.hat.straw_post", pet.Revision)).Content.ReadFromJsonAsync<EquipResult>(Web))!;
        var second = (await (await Equip(c, "head", "equip.hat.knit_post", first.Revision)).Content.ReadFromJsonAsync<EquipResult>(Web))!;
        Assert.Equal("equip.hat.knit_post", second.Equipment["head"]);
        var inv = (await c.GetFromJsonAsync<InventoryResponse>("/api/v1/inventory", Web))!;
        Assert.Equal(new[] { "equip.hat.knit_post" }, inv.Items.Where(i => i.Equipped).Select(i => i.ItemId));
    }

    [Fact]
    public async Task A_costume_and_a_hat_push_each_other_off_but_shoes_stay()
    {
        using var f = new AuthFactory();
        var (c, pet, owner) = await Player(f);
        foreach (var id in new[] { "equip.hat.straw_post", "equip.costume.dog", "equip.feet.sneaker_picnic" }) await Give(f, owner, id);

        var r1 = (await (await Equip(c, "head", "equip.hat.straw_post", pet.Revision)).Content.ReadFromJsonAsync<EquipResult>(Web))!;
        var r2 = (await (await Equip(c, "feet", "equip.feet.sneaker_picnic", r1.Revision)).Content.ReadFromJsonAsync<EquipResult>(Web))!;
        var r3 = (await (await Equip(c, "costume", "equip.costume.dog", r2.Revision)).Content.ReadFromJsonAsync<EquipResult>(Web))!;
        Assert.Equal("equip.costume.dog", r3.Equipment["costume"]);
        Assert.Null(r3.Equipment["head"]);                                      // the costume already has the head
        Assert.Equal("equip.feet.sneaker_picnic", r3.Equipment["feet"]);

        var r4 = (await (await Equip(c, "head", "equip.hat.straw_post", r3.Revision)).Content.ReadFromJsonAsync<EquipResult>(Web))!;
        Assert.Null(r4.Equipment["costume"]);
        Assert.Equal("equip.hat.straw_post", r4.Equipment["head"]);
    }

    [Fact]
    public async Task Stale_revisions_conflict_and_the_database_blocks_two_items_in_one_slot()
    {
        using var f = new AuthFactory();
        var (c, pet, owner) = await Player(f);
        await Give(f, owner, "equip.hat.straw_post");
        Assert.Equal(HttpStatusCode.OK, (await Equip(c, "head", "equip.hat.straw_post", pet.Revision)).StatusCode);
        var stale = await Equip(c, "head", null, pet.Revision);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(ErrorCodes.RevisionConflict, (await Err(stale)).Code);

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inv = await db.InventoryEntries.FirstAsync();
        db.PetEquipment.Add(new PetEquipment { PetId = pet.PetId, Slot = "head", InventoryId = inv.InventoryId, ItemId = inv.ItemId });
        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());   // (pet_id, slot) is the primary key
    }

    [Fact]
    public async Task Players_cannot_wear_each_others_items_and_a_deleted_player_leaves_nothing_behind()
    {
        using var f = new AuthFactory();
        var (a, aPet, aOwner) = await Player(f, "a@example.com");
        var (b, bPet, _) = await Player(f, "b@example.com");
        await Give(f, aOwner, "equip.hat.straw_post");
        Assert.Equal(HttpStatusCode.NotFound, (await Equip(b, "head", "equip.hat.straw_post", bPet.Revision)).StatusCode);

        await Equip(a, "head", "equip.hat.straw_post", aPet.Revision);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Remove(await db.Users.FirstAsync(u => u.Id == aOwner));
        await db.SaveChangesAsync();
        Assert.Equal(0, await db.PetEquipment.CountAsync(e => e.PetId == aPet.PetId));
        Assert.Equal(0, await db.InventoryEntries.CountAsync(i => i.OwnerId == aOwner));
    }

    [Fact]
    public async Task Dev_grant_is_not_available_outside_Development()
    {
        using var f = new AuthFactory();
        var (c, _, _) = await Player(f);
        var res = await c.PostAsJsonAsync("/api/v1/dev/inventory/grant", new { itemId = "equip.hat.straw_post", quantity = 1 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
