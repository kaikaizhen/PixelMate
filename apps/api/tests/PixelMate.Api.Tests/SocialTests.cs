using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Items;

namespace PixelMate.Api.Tests;

public class SocialTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private const string Paper = "item.stationery.letter_border_garden";
    private const string Hat = "equip.hat.straw_post";

    private sealed record Player(HttpClient C, Guid Id, string Name);

    private static async Task<Player> Join(AuthFactory f, string email, string name)
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email, name: name);
        var me = await (await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" })).Content.ReadFromJsonAsync<JsonElement>(Web);
        return new Player(c, me.GetProperty("userId").GetGuid(), name);
    }

    private static async Task<(Player A, Player B)> Friends(AuthFactory f, string kind = "friend")
    {
        var a = await Join(f, "a@example.com", "小栗");
        var b = await Join(f, "b@example.com", "阿福");
        await Pair(a, b, kind);
        return (a, b);
    }

    private static async Task<string> Invite(Player from, string kind = "friend") =>
        (await (await from.C.PostAsJsonAsync("/api/v1/pairing/invites", new { kind })).Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("code").GetString()!;

    private static async Task Pair(Player a, Player b, string kind = "friend")
    {
        var r = await b.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code = await Invite(a, kind) });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    private static async Task<ApiError> Err(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<ApiError>(Web))!;

    private static async Task Give(AuthFactory f, Guid owner, string itemId, int qty = 1)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.InventoryEntries.FirstOrDefaultAsync(i => i.OwnerId == owner && i.ItemId == itemId);
        if (row is null) db.InventoryEntries.Add(new InventoryEntry { OwnerId = owner, ItemId = itemId, Quantity = qty });
        else row.Quantity += qty;
        await db.SaveChangesAsync();
    }

    private static async Task<int> Qty(AuthFactory f, Guid owner, string itemId)
    {
        using var scope = f.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().InventoryEntries.Where(i => i.OwnerId == owner && i.ItemId == itemId).Select(i => i.Quantity).FirstOrDefaultAsync();
    }

    private static Task<HttpResponseMessage> Send(Player from, Guid to, string body = "今天路過花店，想到你。", string? paper = null, string? gift = null, string? key = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mail") { Content = JsonContent.Create(new { recipientUserId = to, body, paperItemId = paper, giftItemId = gift }) };
        req.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return from.C.SendAsync(req);
    }

    private static async Task<Guid> Sent(Player from, Guid to, string? gift = null, string? paper = null)
    {
        var r = await Send(from, to, gift: gift, paper: paper);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("letterId").GetGuid();
    }

    private static async Task<JsonElement> Inbox(Player p, string query = "") => await p.C.GetFromJsonAsync<JsonElement>("/api/v1/mail" + query, Web);

    // ---- pairing -----------------------------------------------------------------------------------------------------
    [Fact]
    public async Task An_invite_code_is_shown_once_previewed_by_the_friend_and_never_by_its_owner()
    {
        using var f = new AuthFactory();
        var a = await Join(f, "a@example.com", "小栗");
        var b = await Join(f, "b@example.com", "阿福");
        var code = await Invite(a);
        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}$", code);

        var list = await a.C.GetStringAsync("/api/v1/pairing/invites");
        Assert.DoesNotContain(code, list);                                                 // only a hash is kept: the list cannot show it again
        Assert.Contains("friend", list);

        var preview = await (await b.C.PostAsJsonAsync("/api/v1/pairing/preview", new { code = code.ToLower().Replace("-", " ") })).Content.ReadFromJsonAsync<JsonElement>(Web);
        Assert.Equal("小栗", preview.GetProperty("inviterName").GetString());
        Assert.Equal("friend", preview.GetProperty("kind").GetString());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await a.C.PostAsJsonAsync("/api/v1/pairing/preview", new { code })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await a.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code })).StatusCode);
        var nope = await b.C.PostAsJsonAsync("/api/v1/pairing/preview", new { code = "AAAA-AAAA" });
        Assert.Equal(HttpStatusCode.NotFound, nope.StatusCode);
    }

    [Fact]
    public async Task Accepting_makes_both_players_friends_and_a_code_works_once()
    {
        using var f = new AuthFactory();
        var a = await Join(f, "a@example.com", "小栗");
        var b = await Join(f, "b@example.com", "阿福");
        var c = await Join(f, "c@example.com", "小米");
        var code = await Invite(a);
        Assert.Equal(HttpStatusCode.OK, (await b.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code })).StatusCode);

        foreach (var (me, other) in new[] { (a, b), (b, a) })
        {
            var rels = (await me.C.GetFromJsonAsync<JsonElement>("/api/v1/pairing/relationships", Web)).GetProperty("relationships");
            Assert.Equal(1, rels.GetArrayLength());
            Assert.Equal(other.Id, rels[0].GetProperty("other").GetProperty("userId").GetGuid());
            Assert.Equal(other.Name, rels[0].GetProperty("other").GetProperty("displayName").GetString());
        }
        Assert.Equal(0, (await c.C.GetFromJsonAsync<JsonElement>("/api/v1/pairing/relationships", Web)).GetProperty("relationships").GetArrayLength());

        var again = await b.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code = await Invite(a) });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);                 // already friends
    }

    [Fact]
    public async Task Invites_expire_and_can_be_taken_back()
    {
        using var f = new AuthFactory();
        var a = await Join(f, "a@example.com", "小栗");
        var b = await Join(f, "b@example.com", "阿福");
        var old = await Invite(a);
        f.Clock.Advance(TimeSpan.FromHours(25));
        await a.C.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" });
        await b.C.PostAsJsonAsync("/api/v1/auth/login", new { email = "b@example.com", password = "Passw0rd-123" });
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code = old })).StatusCode);

        var created = await (await a.C.PostAsJsonAsync("/api/v1/pairing/invites", new { kind = "friend" })).Content.ReadFromJsonAsync<JsonElement>(Web);
        var id = created.GetProperty("inviteId").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.DeleteAsync($"/api/v1/pairing/invites/{id}")).StatusCode);   // not b's invite
        Assert.Equal(HttpStatusCode.NoContent, (await a.C.DeleteAsync($"/api/v1/pairing/invites/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code = created.GetProperty("code").GetString() })).StatusCode);
    }

    [Fact]
    public async Task One_couple_per_person_and_a_friend_limit()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["Letters:MaxRelationships"] = "2" });
        var (a, b) = await Friends(f, "couple");
        var c = await Join(f, "c@example.com", "小米");
        var d = await Join(f, "d@example.com", "阿呆");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await a.C.PostAsJsonAsync("/api/v1/pairing/invites", new { kind = "couple" })).StatusCode);   // a already has one
        var fromC = await Invite(c, "couple");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await b.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code = fromC })).StatusCode);       // b already has one

        await Pair(c, a);                                                                      // friends are fine: a now has 2
        var fromD = await Invite(d);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await a.C.PostAsJsonAsync("/api/v1/pairing/accept", new { code = fromD })).StatusCode);       // the limit
    }

    [Fact]
    public async Task Ending_a_friendship_stops_new_letters_keeps_old_ones_and_allows_pairing_again()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["Letters:MinDelaySeconds"] = "0", ["Letters:MaxDelaySeconds"] = "0" });
        var (a, b) = await Friends(f);
        await Sent(a, b.Id);
        var rel = (await a.C.GetFromJsonAsync<JsonElement>("/api/v1/pairing/relationships", Web)).GetProperty("relationships")[0].GetProperty("relationshipId").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await b.C.DeleteAsync($"/api/v1/pairing/relationships/{rel}")).StatusCode);      // either side may end it
        Assert.Equal(HttpStatusCode.NotFound, (await a.C.DeleteAsync($"/api/v1/pairing/relationships/{rel}")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await Send(a, b.Id)).StatusCode);
        Assert.Equal(1, (await Inbox(b)).GetProperty("letters").GetArrayLength());           // the delivered letter stays in b's mailbox
        Assert.Equal(0, (await a.C.GetFromJsonAsync<JsonElement>("/api/v1/pairing/relationships", Web)).GetProperty("relationships").GetArrayLength());

        await Pair(a, b);
        Assert.Equal(HttpStatusCode.Created, (await Send(a, b.Id)).StatusCode);
    }

    [Fact]
    public async Task Guessing_codes_is_rate_limited()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["RateLimit:PairingPerMinute"] = "3" });
        var b = await Join(f, "b@example.com", "阿福");
        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++) codes.Add((await b.C.PostAsJsonAsync("/api/v1/pairing/preview", new { code = "ABCD-EFGH" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, codes[^1]);
        Assert.Equal(HttpStatusCode.NotFound, codes[0]);
    }

    // ---- letters -----------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Only_friends_can_write_and_a_send_needs_an_idempotency_key()
    {
        using var f = new AuthFactory();
        var a = await Join(f, "a@example.com", "小栗");
        var b = await Join(f, "b@example.com", "阿福");
        var r = await Send(a, b.Id);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal(ErrorCodes.NotConnected, (await Err(r)).Code);
        await Pair(a, b);

        var noKey = await a.C.PostAsJsonAsync("/api/v1/mail", new { recipientUserId = b.Id, body = "hi" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noKey.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(a, a.Id)).StatusCode);
    }

    [Fact]
    public async Task A_letter_arrives_after_the_postmans_walk_then_can_be_read_once()
    {
        using var f = new AuthFactory();
        var (a, b) = await Friends(f);
        var id = await Sent(a, b.Id);

        Assert.Equal(0, (await Inbox(b)).GetProperty("letters").GetArrayLength());
        Assert.Equal(0, (await b.C.GetFromJsonAsync<JsonElement>("/api/v1/mail/unread", Web)).GetProperty("unread").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.GetAsync($"/api/v1/mail/{id}")).StatusCode);          // still on the road
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsync($"/api/v1/mail/{id}/read", null)).StatusCode);
        var sent = (await Inbox(a, "?box=sent")).GetProperty("letters")[0];
        Assert.False(sent.GetProperty("delivered").GetBoolean());                                              // the sender sees it on its way

        f.Clock.Advance(TimeSpan.FromMinutes(16));                                                             // the longest walk is 15 minutes
        var box = await Inbox(b);
        Assert.Equal(1, box.GetProperty("unread").GetInt32());
        Assert.Equal("小栗", box.GetProperty("letters")[0].GetProperty("otherDisplayName").GetString());
        var full = await b.C.GetFromJsonAsync<JsonElement>($"/api/v1/mail/{id}", Web);
        Assert.Equal("今天路過花店，想到你。", full.GetProperty("body").GetString());

        var read1 = await (await b.C.PostAsync($"/api/v1/mail/{id}/read", null)).Content.ReadFromJsonAsync<JsonElement>(Web);
        var read2 = await (await b.C.PostAsync($"/api/v1/mail/{id}/read", null)).Content.ReadFromJsonAsync<JsonElement>(Web);
        Assert.Equal(read1.GetProperty("readAt").GetDateTime(), read2.GetProperty("readAt").GetDateTime());   // idempotent
        Assert.Equal(0, (await Inbox(b)).GetProperty("unread").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await a.C.PostAsync($"/api/v1/mail/{id}/read", null)).StatusCode);   // only the recipient can mark it read
    }

    [Fact]
    public async Task Sending_twice_with_the_same_key_is_one_letter()
    {
        using var f = new AuthFactory();
        var (a, b) = await Friends(f);
        var r1 = await (await Send(a, b.Id, key: "same-key-0001")).Content.ReadFromJsonAsync<JsonElement>(Web);
        var r2 = await (await Send(a, b.Id, body: "something else", key: "same-key-0001")).Content.ReadFromJsonAsync<JsonElement>(Web);
        Assert.Equal(r1.GetProperty("letterId").GetGuid(), r2.GetProperty("letterId").GetGuid());
        Assert.Equal("今天路過花店，想到你。", r2.GetProperty("body").GetString());
        Assert.Equal(1, (await Inbox(a, "?box=sent")).GetProperty("letters").GetArrayLength());
    }

    [Fact]
    public async Task A_gift_leaves_the_sender_at_once_and_reaches_the_recipient_exactly_once_when_opened()
    {
        using var f = new AuthFactory();
        var (a, b) = await Friends(f);
        await Give(f, a.Id, Hat, 2);

        var id = await Sent(a, b.Id, gift: Hat);
        Assert.Equal(1, await Qty(f, a.Id, Hat));
        Assert.Equal(0, await Qty(f, b.Id, Hat));

        f.Clock.Advance(TimeSpan.FromMinutes(16));
        await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ => { try { await b.C.PostAsync($"/api/v1/mail/{id}/read", null); } catch (Exception) { /* one shared test connection */ } }));
        await b.C.PostAsync($"/api/v1/mail/{id}/read", null);
        Assert.Equal(1, await Qty(f, b.Id, Hat));                                                              // never two
        var bag = await b.C.GetStringAsync("/api/v1/inventory");
        Assert.Contains(Hat, bag);
    }

    [Fact]
    public async Task Gifts_and_paper_must_be_owned_and_a_worn_last_copy_cannot_be_given()
    {
        using var f = new AuthFactory();
        var (a, b) = await Friends(f);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(a, b.Id, gift: Hat)).StatusCode);        // not in the bag
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(a, b.Id, paper: Paper)).StatusCode);
        await Give(f, a.Id, Hat);
        await Give(f, a.Id, Paper);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(a, b.Id, paper: Hat)).StatusCode);       // paper must be stationery
        Assert.Equal(HttpStatusCode.Created, (await Send(a, b.Id, paper: Paper)).StatusCode);
        Assert.Equal(1, await Qty(f, a.Id, Paper));                                                            // paper is not used up

        await a.C.PostAsJsonAsync("/api/v1/pets", new { name = "小栗", bodyVariant = "star_01_cream" });
        var pet = await a.C.GetFromJsonAsync<JsonElement>("/api/v1/pets/me", Web);
        var wear = await a.C.PostAsJsonAsync("/api/v1/pets/me/equipment", new { slot = "head", itemId = Hat, expectedRevision = pet.GetProperty("revision").GetInt32() });
        Assert.Equal(HttpStatusCode.OK, wear.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(a, b.Id, gift: Hat)).StatusCode);        // it is on the pet
        Assert.Equal(1, await Qty(f, a.Id, Hat));
    }

    [Fact]
    public async Task Nobody_else_can_see_touch_or_delete_a_letter()
    {
        using var f = new AuthFactory();
        var (a, b) = await Friends(f);
        var c = await Join(f, "c@example.com", "小米");
        var id = await Sent(a, b.Id);
        f.Clock.Advance(TimeSpan.FromMinutes(16));

        Assert.Equal(HttpStatusCode.NotFound, (await c.C.GetAsync($"/api/v1/mail/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.C.PostAsync($"/api/v1/mail/{id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.C.DeleteAsync($"/api/v1/mail/{id}")).StatusCode);
        Assert.Equal(0, (await Inbox(c)).GetProperty("letters").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await a.C.GetAsync($"/api/v1/mail/{id}")).StatusCode);              // the sender can look at their own
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_empty_letter_is_refused(string body)
    {
        using var f = new AuthFactory();
        var (a, b) = await Friends(f);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(a, b.Id, body)).StatusCode);
    }

    [Fact]
    public async Task Letters_are_plain_text_up_to_500_characters()
    {
        using var f = new AuthFactory();
        var (a, b) = await Friends(f);
        Assert.Equal(HttpStatusCode.Created, (await Send(a, b.Id, new string('字', 500))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(a, b.Id, new string('字', 501))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(a, b.Id, "bell\u0007")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Send(a, b.Id, "第一行\n第二行 <b>bold</b> 🌟")).StatusCode);          // markup is just text; the apps never render HTML
    }

    [Fact]
    public async Task Deleting_follows_the_rules_and_the_row_goes_when_both_sides_are_done()
    {
        using var f = new AuthFactory();
        var (a, b) = await Friends(f);
        var id = await Sent(a, b.Id, gift: null);
        f.Clock.Advance(TimeSpan.FromMinutes(16));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await b.C.DeleteAsync($"/api/v1/mail/{id}")).StatusCode);   // read it first
        await b.C.PostAsync($"/api/v1/mail/{id}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, (await b.C.DeleteAsync($"/api/v1/mail/{id}")).StatusCode);
        Assert.Equal(0, (await Inbox(b)).GetProperty("letters").GetArrayLength());
        Assert.Equal(1, (await Inbox(a, "?box=sent")).GetProperty("letters").GetArrayLength());                      // the sender's copy is theirs
        Assert.Equal(HttpStatusCode.NoContent, (await a.C.DeleteAsync($"/api/v1/mail/{id}")).StatusCode);
        using var scope = f.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Letters.AnyAsync());
    }

    [Fact]
    public async Task A_sender_has_a_daily_limit_and_the_inbox_pages()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["Letters:MaxPerDay"] = "3", ["Letters:MinDelaySeconds"] = "0", ["Letters:MaxDelaySeconds"] = "0" });
        var (a, b) = await Friends(f);
        for (var i = 0; i < 3; i++) { await Sent(a, b.Id); f.Clock.Advance(TimeSpan.FromSeconds(10)); }
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Send(a, b.Id)).StatusCode);

        var page1 = await Inbox(b, "?limit=2");
        Assert.Equal(2, page1.GetProperty("letters").GetArrayLength());
        var next = page1.GetProperty("nextCursor").GetString();
        Assert.NotNull(next);
        var page2 = await Inbox(b, $"?limit=2&cursor={next}");
        Assert.Equal(1, page2.GetProperty("letters").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, page2.GetProperty("nextCursor").ValueKind);

        f.Clock.Advance(TimeSpan.FromHours(25));                                                               // a day later the limit is gone
        await a.C.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" });
        Assert.Equal(HttpStatusCode.Created, (await Send(a, b.Id)).StatusCode);
    }
}
