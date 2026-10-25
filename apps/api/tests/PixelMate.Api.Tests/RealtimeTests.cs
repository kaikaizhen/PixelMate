using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PixelMate.Api.Data;
using PixelMate.Api.Items;
using PixelMate.Api.Realtime;

namespace PixelMate.Api.Tests;

public class RealtimeTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>The SignalR .NET client always sends X-Requested-With: XMLHttpRequest; the browser client lets the app override it, so do the same here.</summary>
    private sealed class Marker(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            r.Headers.Remove("X-Requested-With"); r.Headers.Add("X-Requested-With", "pixelmate");
            return base.SendAsync(r, ct);
        }
    }

    private sealed record Player(HttpClient C, Guid Id, string Cookie);

    private static async Task<Player> Join(AuthFactory f, string email, string name = "小栗")
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email, name: name);
        var res = await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" });
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("userId").GetGuid();
        var cookie = string.Join("; ", res.Headers.GetValues("Set-Cookie").Select(v => v.Split(';')[0]));
        await c.PostAsJsonAsync("/api/v1/pets", new { name, bodyVariant = "star_01_cream" });
        return new Player(c, id, cookie);
    }

    /// <summary>A real SignalR client against the in-memory server (long polling: no socket needed), signed in with the player's cookie.</summary>
    private static async Task<(HubConnection Hub, ConcurrentQueue<JsonElement> Got)> Listen(AuthFactory f, Player p)
    {
        var hub = new HubConnectionBuilder().WithUrl(new Uri(f.Server.BaseAddress, "/api/v1/hub"), o =>
        {
            o.HttpMessageHandlerFactory = _ => new Marker(f.Server.CreateHandler());
            o.Transports = HttpTransportType.LongPolling;
            o.Headers["Cookie"] = p.Cookie;
            o.Headers["X-Requested-With"] = "pixelmate";
        }).Build();
        var got = new ConcurrentQueue<JsonElement>();
        hub.On<JsonElement>("pet.event", e => got.Enqueue(e));
        await hub.StartAsync();
        await Task.Delay(300);                       // let the first poll finish its own database work before the test makes calls (one shared SQLite connection)
        return (hub, got);
    }

    private static OutboxDispatcher Dispatcher(AuthFactory f) => f.Services.GetRequiredService<OutboxDispatcher>();

    private static async Task<JsonElement> Next(ConcurrentQueue<JsonElement> got, string? type = null)
    {
        for (var i = 0; i < 100; i++)
        {
            while (got.TryDequeue(out var e)) if (type is null || e.GetProperty("type").GetString() == type) return e;
            await Task.Delay(50);
        }
        throw new TimeoutException($"no {type ?? "event"} arrived");
    }

    private static async Task<int> Revision(HttpClient c) => (await c.GetFromJsonAsync<JsonElement>("/api/v1/pets/me", Web)).GetProperty("revision").GetInt32();

    [Fact]
    public async Task The_hub_needs_a_signed_in_player()
    {
        using var f = new AuthFactory();
        f.NewClient();
        var anon = f.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/hub/negotiate?negotiateVersion=1");
        req.Headers.Add("X-Requested-With", "pixelmate");
        var r = await anon.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Changing_the_pet_on_one_device_tells_the_players_other_devices_and_nobody_else()
    {
        using var f = new AuthFactory();
        var a = await Join(f, "a@example.com");
        var b = await Join(f, "b@example.com", "阿福");
        await using var otherDevice = (await Listen(f, a)).Hub;
        var (hubB, gotB) = await Listen(f, b);
        await using var _ = hubB;
        var (hubA, gotA) = await Listen(f, a);
        await using var __ = hubA;

        var rev = await Revision(a.C);
        var res = await a.C.PostAsJsonAsync("/api/v1/pets/me/skills/reset", new { expectedRevision = rev });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        await Dispatcher(f).DispatchOnce(default);

        var e = await Next(gotA, "pet.updated");
        Assert.Equal(rev + 1, e.GetProperty("revision").GetInt32());
        Assert.Equal("skills", e.GetProperty("payload").GetProperty("reason").GetString());
        Assert.True(e.GetProperty("eventId").GetGuid() != Guid.Empty);
        await Task.Delay(300);
        Assert.Empty(gotB);                                                     // b never hears about a's pet
    }

    [Fact]
    public async Task Each_event_is_pushed_once_and_marked_as_sent()
    {
        using var f = new AuthFactory();
        var a = await Join(f, "a@example.com");
        var (hub, got) = await Listen(f, a);
        await using var _ = hub;
        await a.C.PostAsJsonAsync("/api/v1/pets/me/skills/reset", new { expectedRevision = await Revision(a.C) });

        Assert.Equal(1, await Dispatcher(f).DispatchOnce(default));
        Assert.Equal(0, await Dispatcher(f).DispatchOnce(default));
        await Next(got, "pet.updated");
        await Task.Delay(300);
        Assert.Empty(got);
        using var scope = f.Services.CreateScope();
        Assert.All(await scope.ServiceProvider.GetRequiredService<AppDbContext>().OutboxEvents.ToListAsync(), e => Assert.NotNull(e.DispatchedAt));
    }

    [Fact]
    public async Task A_letter_is_announced_when_the_postman_arrives_not_when_it_is_sent()
    {
        using var f = new AuthFactory();
        var a = await Join(f, "a@example.com");
        var b = await Join(f, "b@example.com", "阿福");
        var code = (await (await a.C.PostAsJsonAsync("/api/v1/pairing/invites", new { kind = "friend" })).Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("code").GetString();
        var acc = await b.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code });
        Assert.Equal(HttpStatusCode.OK, acc.StatusCode);

        var (hub, got) = await Listen(f, b);
        await using var _ = hub;
        await Dispatcher(f).DispatchOnce(default);
        var friends = await Next(got, "friends.updated");                      // pairing tells both sides
        Assert.NotNull(friends.GetProperty("eventId").GetString());

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mail") { Content = JsonContent.Create(new { recipientUserId = b.Id, body = "hi" }) };
        req.Headers.Add("Idempotency-Key", "realtime-key-1");
        var letterId = (await (await a.C.SendAsync(req)).Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("letterId").GetGuid();

        await Dispatcher(f).DispatchOnce(default);
        await Task.Delay(300);
        Assert.Empty(got);                                                      // still on the road

        f.Clock.Advance(TimeSpan.FromMinutes(16));
        await Dispatcher(f).DispatchOnce(default);
        var e = await Next(got, "mail.received");
        Assert.Equal(letterId, e.GetProperty("payload").GetProperty("letterId").GetGuid());

        await Dispatcher(f).DispatchOnce(default);                              // announced once only
        await Task.Delay(300);
        Assert.Empty(got);
    }

    [Fact]
    public async Task A_device_that_was_away_can_ask_what_happened_after_the_last_event_it_saw()
    {
        using var f = new AuthFactory();
        var a = await Join(f, "a@example.com");
        var b = await Join(f, "b@example.com", "阿福");
        await a.C.PostAsJsonAsync("/api/v1/pets/me/skills/reset", new { expectedRevision = await Revision(a.C) });
        await a.C.PostAsJsonAsync("/api/v1/pets/me/skills/reset", new { expectedRevision = await Revision(a.C) });
        await b.C.PostAsJsonAsync("/api/v1/pets/me/skills/reset", new { expectedRevision = await Revision(b.C) });

        var all = await a.C.GetFromJsonAsync<JsonElement>("/api/v1/events", Web);
        var events = all.GetProperty("events");
        Assert.Equal(2, events.GetArrayLength());                               // a's own, not b's
        var first = events[0].GetProperty("seq").GetInt64();
        Assert.True(first < events[1].GetProperty("seq").GetInt64());

        var later = await a.C.GetFromJsonAsync<JsonElement>($"/api/v1/events?after={first}", Web);
        Assert.Equal(1, later.GetProperty("events").GetArrayLength());
        Assert.Equal(all.GetProperty("latestSeq").GetInt64(), later.GetProperty("latestSeq").GetInt64());
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().GetAsync("/api/v1/events")).StatusCode);
    }

    [Fact]
    public async Task Wearing_things_saving_the_room_and_finding_items_all_say_so()
    {
        using var f = new AuthFactory();
        var a = await Join(f, "a@example.com");
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.InventoryEntries.Add(new InventoryEntry { OwnerId = a.Id, ItemId = "equip.hat.straw_post", Quantity = 1 });
            await db.SaveChangesAsync();
        }
        await a.C.PostAsJsonAsync("/api/v1/pets/me/equipment", new { slot = "head", itemId = "equip.hat.straw_post", expectedRevision = await Revision(a.C) });
        var room = await a.C.GetFromJsonAsync<JsonElement>("/api/v1/room", Web);
        await a.C.PutAsJsonAsync("/api/v1/room", new { expectedRevision = room.GetProperty("revision").GetInt32(), floorItemId = (string?)null, placements = Array.Empty<object>() });
        f.Clock.Advance(TimeSpan.FromDays(5));
        await a.C.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" });
        await a.C.GetAsync("/api/v1/pets/me");                                  // the lazy catch-up makes finds

        var types = (await a.C.GetFromJsonAsync<JsonElement>("/api/v1/events", Web)).GetProperty("events").EnumerateArray().Select(e => e.GetProperty("type").GetString()).ToHashSet();
        Assert.Contains("pet.updated", types);
        Assert.Contains("room.updated", types);
        Assert.Contains("pet.find", types);
        Assert.Contains("inventory.updated", types);
    }
}
