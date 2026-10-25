using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PixelMate.Api.Data;
using PixelMate.Api.Items;
using PixelMate.Api.Rooms;

namespace PixelMate.Api.Tests;

public class RoomTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private const string Table = RoomRules.StarterItem;
    private const string Bed = "world.furniture.bed_garden";
    private const string Plant = "world.decor.bouquet_garden";
    private const string Shell = "item.collectible.shell_seaside";
    private const string Floor = "world.decor.floor_tile_garden";
    private const string Hat = "equip.hat.straw_post";

    private static async Task<(HttpClient C, Guid Id)> Join(AuthFactory f, string email = "a@example.com")
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email);
        var me = await (await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" })).Content.ReadFromJsonAsync<JsonElement>(Web);
        return (c, me.GetProperty("userId").GetGuid());
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

    private static async Task<RoomDto> Room(HttpClient c) => (await c.GetFromJsonAsync<RoomDto>("/api/v1/room", Web))!;

    private static Task<HttpResponseMessage> Save(HttpClient c, int rev, object[] placements, string? floor = null) =>
        c.PutAsJsonAsync("/api/v1/room", new { expectedRevision = rev, floorItemId = floor, placements });

    private static object P(string item, int x, int y) => new { itemId = item, x, y };

    [Fact]
    public async Task The_first_look_gives_the_starter_table_once_and_it_is_the_players_own()
    {
        using var f = new AuthFactory();
        var (c, me) = await Join(f);
        var room = await Room(c);
        Assert.Equal(1, room.Revision);
        var table = Assert.Single(room.Placements);
        Assert.Equal((Table, RoomRules.StarterX, RoomRules.StarterY), (table.ItemId, table.X, table.Y));
        Assert.Contains(Table, await c.GetStringAsync("/api/v1/inventory"));

        var again = await Room(c);                                            // reading again adds nothing
        Assert.Equal(room.Placements[0].PlacementId, again.Placements[0].PlacementId);
        using var scope = f.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<AppDbContext>().InventoryEntries.Where(i => i.OwnerId == me && i.ItemId == Table).SumAsync(i => i.Quantity));
    }

    [Fact]
    public async Task Owned_items_can_be_placed_moved_and_the_starter_table_can_be_taken_away()
    {
        using var f = new AuthFactory();
        var (c, me) = await Join(f);
        await Give(f, me, Bed); await Give(f, me, Plant); await Give(f, me, Shell); await Give(f, me, Floor);
        var rev = (await Room(c)).Revision;

        var r = await Save(c, rev, [P(Table, 20, 110), P(Bed, 30, 90), P(Plant, 120, 150), P(Shell, 200, 170)], Floor);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var room = (await r.Content.ReadFromJsonAsync<RoomDto>(Web))!;
        Assert.Equal(2, room.Revision);
        Assert.Equal(Floor, room.FloorItemId);
        Assert.Equal(new[] { 90, 110, 150, 170 }, room.Placements.Select(p => p.Y));        // saved and returned top to bottom
        Assert.Equal(room.Placements.Select(p => p.PlacementId), (await Room(c)).Placements.Select(p => p.PlacementId));

        var cleared = (await (await Save(c, 2, [])).Content.ReadFromJsonAsync<RoomDto>(Web))!;   // everything may be put away, even the starter table
        Assert.Empty(cleared.Placements);
        Assert.Null(cleared.FloorItemId);
        Assert.Contains(Table, await c.GetStringAsync("/api/v1/inventory"));                      // still owned
    }

    [Fact]
    public async Task Only_owned_placeable_items_in_range_and_never_more_copies_than_owned()
    {
        using var f = new AuthFactory();
        var (c, me) = await Join(f);
        await Give(f, me, Hat); await Give(f, me, Floor); await Give(f, me, Bed);
        var rev = (await Room(c)).Revision;

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, [P(Plant, 10, 10)])).StatusCode);                 // not owned
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, [P(Hat, 10, 10)])).StatusCode);                   // a wearable
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, [P(Floor, 10, 10)])).StatusCode);                 // floors go to the floor slot
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, [P("no.such.item", 10, 10)])).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, [P(Bed, 10, 10), P(Bed, 60, 10)])).StatusCode);   // one bed owned
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, [P(Bed, 10, 10)], Bed)).StatusCode);              // a bed is no floor
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, [], "world.decor.floor_tile_forest")).StatusCode); // floor not owned
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, [P(Bed, -1, 10)])).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, [P(Bed, 10, 241)])).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Save(c, rev, Enumerable.Range(0, 41).Select(i => P(Table, i, i)).ToArray())).StatusCode);
        Assert.Equal(rev, (await Room(c)).Revision);                                                                                   // nothing was written
        Assert.Equal(HttpStatusCode.OK, (await Save(c, rev, [P(Bed, 0, 0), P(Table, 240, 240)])).StatusCode);                  // the edges are inside
    }

    [Fact]
    public async Task Two_devices_saving_from_the_same_version_cannot_overwrite_each_other()
    {
        using var f = new AuthFactory();
        var (c, me) = await Join(f);
        await Give(f, me, Bed);
        var rev = (await Room(c)).Revision;
        Assert.Equal(HttpStatusCode.OK, (await Save(c, rev, [P(Table, 1, 1)])).StatusCode);
        var stale = await Save(c, rev, [P(Bed, 5, 5)]);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(Table, Assert.Single((await Room(c)).Placements).ItemId);
    }

    [Fact]
    public async Task Every_player_has_their_own_room()
    {
        using var f = new AuthFactory();
        var (a, ida) = await Join(f, "a@example.com");
        var (b, _) = await Join(f, "b@example.com");
        await Give(f, ida, Bed);
        await Save(a, (await Room(a)).Revision, [P(Bed, 40, 40)]);
        var rb = await Room(b);
        Assert.Equal(Table, Assert.Single(rb.Placements).ItemId);
        Assert.Equal((await Room(b)).Placements[0].PlacementId, rb.Placements[0].PlacementId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().GetAsync("/api/v1/room")).StatusCode);
    }

    [Fact]
    public async Task An_item_standing_in_the_room_cannot_be_given_away_until_it_is_put_away()
    {
        using var f = new AuthFactory();
        var (a, ida) = await Join(f, "a@example.com");
        var (b, idb) = await Join(f, "b@example.com");
        var code = (await (await a.PostAsJsonAsync("/api/v1/pairing/invites", new { kind = "friend" })).Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("code").GetString();
        await b.PostAsJsonAsync("/api/v1/pairing/accept", new { code });

        var rev = (await Room(a)).Revision;                                   // a owns exactly the table, and it stands in the room
        HttpRequestMessage Gift() { var m = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mail") { Content = JsonContent.Create(new { recipientUserId = idb, body = "hi", giftItemId = Table }) }; m.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); return m; }
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await a.SendAsync(Gift())).StatusCode);
        await Save(a, rev, []);
        Assert.Equal(HttpStatusCode.Created, (await a.SendAsync(Gift())).StatusCode);
    }
}
