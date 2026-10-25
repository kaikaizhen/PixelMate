using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PixelMate.Api.Data;
using PixelMate.Api.Devices;

namespace PixelMate.Api.Tests;

public class DeviceTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task<(HttpClient C, Guid Id)> Owner(AuthFactory f, string email = "a@example.com", bool pet = true)
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email);
        var me = await (await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" })).Content.ReadFromJsonAsync<JsonElement>(Web);
        if (pet) await c.PostAsJsonAsync("/api/v1/pets", new { name = "小栗", bodyVariant = "star_01_cream" });
        return (c, me.GetProperty("userId").GetGuid());
    }

    /// <summary>A device on the wire: no cookie, no CSRF header, only what an ESP32 has.</summary>
    private static HttpClient Bare(AuthFactory f)
    {
        f.NewClient();
        var h = new HttpClient(f.Server.CreateHandler()) { BaseAddress = f.Server.BaseAddress };
        return h;
    }

    private static async Task<JsonElement> Start(HttpClient device, string model = "Waveshare-AMOLED-2.16")
    {
        var r = await device.PostAsJsonAsync("/api/v1/device/pairing/start", new { model });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>(Web);
    }

    private static Task<HttpResponseMessage> Poll(HttpClient device, JsonElement started) =>
        device.PostAsJsonAsync("/api/v1/device/pairing/poll", new { pairingId = started.GetProperty("pairingId").GetGuid(), pollToken = started.GetProperty("pollToken").GetString() });

    private static async Task<string> PollStatus(HttpClient device, JsonElement started) =>
        (await (await Poll(device, started)).Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("status").GetString()!;

    private static Task<HttpResponseMessage> Approve(HttpClient owner, JsonElement started, string? name = null) =>
        owner.PostAsJsonAsync("/api/v1/devices/pair", new { code = started.GetProperty("code").GetString(), name });

    /// <summary>The whole onboarding: returns a client that sends the collected credential.</summary>
    private static async Task<(HttpClient Device, string Credential, Guid DeviceId)> Pair(AuthFactory f, HttpClient owner, string? name = null)
    {
        var device = Bare(f);
        var started = await Start(device);
        Assert.Equal("pending", await PollStatus(device, started));
        Assert.Equal(HttpStatusCode.OK, (await Approve(owner, started, name)).StatusCode);
        var got = await (await Poll(device, started)).Content.ReadFromJsonAsync<JsonElement>(Web);
        Assert.Equal("approved", got.GetProperty("status").GetString());
        var credential = got.GetProperty("credential").GetString()!;
        device.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        return (device, credential, got.GetProperty("deviceId").GetGuid());
    }

    // ---- onboarding -----------------------------------------------------------------------------------------------------
    [Fact]
    public async Task A_device_pairs_in_three_steps_and_gets_its_credential_exactly_once()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var device = Bare(f);

        var started = await Start(device);
        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}$", started.GetProperty("code").GetString());
        Assert.Equal(3, started.GetProperty("pollIntervalSeconds").GetInt32());
        Assert.Equal("pending", await PollStatus(device, started));

        var ok = await Approve(owner, started, "客廳的小栗");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var dto = await ok.Content.ReadFromJsonAsync<JsonElement>(Web);
        Assert.Equal("waiting", dto.GetProperty("state").GetString());                  // approved, the device has not come for its credential yet

        var got = await (await Poll(device, started)).Content.ReadFromJsonAsync<JsonElement>(Web);
        Assert.Equal("approved", got.GetProperty("status").GetString());
        var credential = got.GetProperty("credential").GetString()!;
        Assert.StartsWith(got.GetProperty("deviceId").GetGuid().ToString("N") + ".", credential);

        Assert.Equal("delivered", await PollStatus(device, started));                    // the credential is never handed out twice
        var list = (await owner.GetFromJsonAsync<JsonElement>("/api/v1/devices", Web)).GetProperty("devices");
        Assert.Equal("active", list[0].GetProperty("state").GetString());
        Assert.Equal("客廳的小栗", list[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Nothing_secret_is_stored_in_the_clear()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var device = Bare(f);
        var started = await Start(device);
        await Approve(owner, started);
        var got = await (await Poll(device, started)).Content.ReadFromJsonAsync<JsonElement>(Web);
        var credential = got.GetProperty("credential").GetString()!;

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var d = await db.Devices.SingleAsync();
        var p = await db.DevicePairings.SingleAsync();
        Assert.Equal(64, d.CredentialHash!.Length);
        Assert.DoesNotContain(credential.Split('.')[1], d.CredentialHash);
        Assert.NotEqual(started.GetProperty("code").GetString()!.Replace("-", ""), p.CodeHash);
        Assert.NotEqual(started.GetProperty("pollToken").GetString(), p.PollTokenHash);
        Assert.Equal(64, p.CodeHash.Length);
    }

    [Fact]
    public async Task Only_the_device_that_started_a_pairing_can_poll_it()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var device = Bare(f);
        var started = await Start(device);
        await Approve(owner, started);

        var thief = await device.PostAsJsonAsync("/api/v1/device/pairing/poll", new { pairingId = started.GetProperty("pairingId").GetGuid(), pollToken = "guessed-token" });
        Assert.Equal(HttpStatusCode.NotFound, thief.StatusCode);
        var nobody = await device.PostAsJsonAsync("/api/v1/device/pairing/poll", new { pairingId = Guid.NewGuid(), pollToken = started.GetProperty("pollToken").GetString() });
        Assert.Equal(HttpStatusCode.NotFound, nobody.StatusCode);                        // the same answer, so neither a wrong id nor a wrong token tells anything
        Assert.Equal("approved", await PollStatus(device, started));                     // the real device still gets it
    }

    [Fact]
    public async Task A_code_expires_works_once_and_needs_a_pet()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var (other, _) = await Owner(f, "b@example.com");
        var (noPet, _) = await Owner(f, "c@example.com", pet: false);
        var device = Bare(f);

        var started = await Start(device);
        var petless = await Approve(noPet, started);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, petless.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Approve(owner, started)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Approve(other, started)).StatusCode);   // someone else typing the same code a moment later

        var late = await Start(device);
        f.Clock.Advance(TimeSpan.FromMinutes(11));
        await owner.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" });
        Assert.Equal(HttpStatusCode.NotFound, (await Approve(owner, late)).StatusCode);
        Assert.Equal("expired", await PollStatus(device, late));
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsJsonAsync("/api/v1/devices/pair", new { code = "AAAA-AAAA" })).StatusCode);
    }

    [Fact]
    public async Task A_pet_has_at_most_three_devices()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        for (var i = 0; i < 3; i++) await Pair(f, owner);
        var extra = Bare(f);
        var started = await Start(extra);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Approve(owner, started)).StatusCode);
    }

    [Fact]
    public async Task Guessing_pairing_codes_and_polling_too_fast_is_limited()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["RateLimit:PairingPerMinute"] = "3", ["RateLimit:DevicePollPerMinute"] = "4" });
        var (owner, _) = await Owner(f);
        var guesses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++) guesses.Add((await owner.PostAsJsonAsync("/api/v1/devices/pair", new { code = "ZZZZ-ZZZZ" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, guesses[^1]);

        var device = Bare(f);
        var polls = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++) polls.Add((await device.PostAsJsonAsync("/api/v1/device/pairing/poll", new { pairingId = Guid.NewGuid(), pollToken = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, polls[^1]);
    }

    // ---- what a paired device may do -------------------------------------------------------------------------------------
    [Fact]
    public async Task A_paired_device_reads_its_pets_snapshot_events_and_mail_without_cookies_or_csrf_headers()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var (device, _, deviceId) = await Pair(f, owner);

        var snap = await device.GetFromJsonAsync<JsonElement>("/api/v1/device/snapshot", Web);
        Assert.Equal("小栗", snap.GetProperty("pet").GetProperty("name").GetString());
        Assert.Equal(0, snap.GetProperty("unreadMail").GetInt32());
        Assert.True(snap.GetProperty("latestEventSeq").GetInt64() >= 1);                 // pairing itself is an event
        Assert.Contains(snap.GetProperty("pet").GetProperty("world").GetProperty("weather").GetString(), new[] { "clear", "cloudy" });

        var events = (await device.GetFromJsonAsync<JsonElement>("/api/v1/device/events", Web)).GetProperty("events");
        Assert.Contains(events.EnumerateArray().Select(e => e.GetProperty("type").GetString()), t => t == "devices.updated");
        var inbox = await device.GetFromJsonAsync<JsonElement>("/api/v1/device/mail", Web);
        Assert.Equal(0, inbox.GetProperty("letters").GetArrayLength());

        var seen = (await owner.GetFromJsonAsync<JsonElement>("/api/v1/devices", Web)).GetProperty("devices")[0];
        Assert.Equal(deviceId, seen.GetProperty("deviceId").GetGuid());
        Assert.NotEqual(JsonValueKind.Null, seen.GetProperty("lastSeenAt").ValueKind);   // it was seen just now
    }

    [Fact]
    public async Task A_device_sees_its_owners_mail_and_can_open_a_letter()
    {
        using var f = new AuthFactory(new Dictionary<string, string?> { ["Letters:MinDelaySeconds"] = "0", ["Letters:MaxDelaySeconds"] = "0" });
        var (owner, ownerId) = await Owner(f, "a@example.com");
        var (friend, friendId) = await Owner(f, "b@example.com");
        var code = (await (await friend.PostAsJsonAsync("/api/v1/pairing/invites", new { kind = "friend" })).Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("code").GetString();
        await owner.PostAsJsonAsync("/api/v1/pairing/accept", new { code });
        var (device, _, _) = await Pair(f, owner);

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mail") { Content = JsonContent.Create(new { recipientUserId = ownerId, body = "給你的信" }) };
        req.Headers.Add("Idempotency-Key", "device-mail-1");
        var letterId = (await (await friend.SendAsync(req)).Content.ReadFromJsonAsync<JsonElement>(Web)).GetProperty("letterId").GetGuid();

        Assert.Equal(1, (await device.GetFromJsonAsync<JsonElement>("/api/v1/device/snapshot", Web)).GetProperty("unreadMail").GetInt32());
        var letters = (await device.GetFromJsonAsync<JsonElement>("/api/v1/device/mail", Web)).GetProperty("letters");
        Assert.Equal(letterId, letters[0].GetProperty("letterId").GetGuid());
        var full = await device.GetFromJsonAsync<JsonElement>($"/api/v1/device/mail/{letterId}", Web);
        Assert.Equal("給你的信", full.GetProperty("body").GetString());
        Assert.Equal(HttpStatusCode.OK, (await device.PostAsync($"/api/v1/device/mail/{letterId}/read", null)).StatusCode);
        Assert.Equal(0, (await device.GetFromJsonAsync<JsonElement>("/api/v1/device/snapshot", Web)).GetProperty("unreadMail").GetInt32());
        Assert.NotEqual(friendId, ownerId);
    }

    [Fact]
    public async Task A_device_credential_opens_only_the_device_routes_and_a_login_cookie_does_not_open_them()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var (device, _, _) = await Pair(f, owner);

        // everything that belongs to the account stays closed to the device
        foreach (var (method, path) in new[] { ("GET", "/api/v1/pets/me"), ("GET", "/api/v1/inventory"), ("GET", "/api/v1/mail"), ("GET", "/api/v1/devices"), ("GET", "/api/v1/room"), ("GET", "/api/v1/auth/me"), ("GET", "/api/v1/me/location") })
        {
            var r = await device.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }
        var wear = await device.PostAsJsonAsync("/api/v1/pets/me/equipment", new { slot = "head", itemId = "x", expectedRevision = 1 });
        Assert.NotEqual(HttpStatusCode.OK, wear.StatusCode);

        // and the signed-in player's cookie is not a device
        Assert.Equal(HttpStatusCode.Unauthorized, (await owner.GetAsync("/api/v1/device/snapshot")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Bare(f).GetAsync("/api/v1/device/snapshot")).StatusCode);
    }

    [Fact]
    public async Task Forged_or_damaged_credentials_are_refused()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var (device, credential, deviceId) = await Pair(f, owner);
        async Task<HttpStatusCode> With(string bearer)
        {
            var r = new HttpRequestMessage(HttpMethod.Get, "/api/v1/device/snapshot");
            r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return (await device.SendAsync(r)).StatusCode;
        }
        Assert.Equal(HttpStatusCode.OK, await With(credential));
        Assert.Equal(HttpStatusCode.Unauthorized, await With(credential[..^2] + "xx"));
        Assert.Equal(HttpStatusCode.Unauthorized, await With(deviceId.ToString("N") + ".AAAAAAAA"));
        Assert.Equal(HttpStatusCode.Unauthorized, await With(Guid.NewGuid().ToString("N") + "." + credential.Split('.')[1]));
        Assert.Equal(HttpStatusCode.Unauthorized, await With("not-a-credential"));
        Assert.Equal(HttpStatusCode.Unauthorized, await With(""));
    }

    // ---- ending it -------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task Revoking_a_device_stops_it_at_once_and_a_new_pairing_is_needed()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var (device, _, deviceId) = await Pair(f, owner);
        Assert.Equal(HttpStatusCode.OK, (await device.GetAsync("/api/v1/device/snapshot")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/devices/{deviceId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await device.GetAsync("/api/v1/device/snapshot")).StatusCode);
        Assert.Equal(0, (await owner.GetFromJsonAsync<JsonElement>("/api/v1/devices", Web)).GetProperty("devices").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"/api/v1/devices/{deviceId}")).StatusCode);

        var (again, _, _) = await Pair(f, owner);                                         // the slot is free again
        Assert.Equal(HttpStatusCode.OK, (await again.GetAsync("/api/v1/device/snapshot")).StatusCode);
    }

    [Fact]
    public async Task Only_the_owner_can_revoke_or_rename_a_device_and_a_device_can_unpair_itself()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f, "a@example.com");
        var (stranger, _) = await Owner(f, "b@example.com");
        var (device, _, deviceId) = await Pair(f, owner);

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/v1/devices/{deviceId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PutAsJsonAsync($"/api/v1/devices/{deviceId}", new { name = "mine now" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PutAsJsonAsync($"/api/v1/devices/{deviceId}", new { name = "書桌" })).StatusCode);
        Assert.Equal("書桌", (await owner.GetFromJsonAsync<JsonElement>("/api/v1/devices", Web)).GetProperty("devices")[0].GetProperty("name").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await device.DeleteAsync("/api/v1/device/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await device.GetAsync("/api/v1/device/snapshot")).StatusCode);
    }

    [Fact]
    public async Task Rotating_the_credential_replaces_it_at_once()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var (device, oldCredential, deviceId) = await Pair(f, owner);

        var rotated = await (await device.PostAsync("/api/v1/device/credential/rotate", null)).Content.ReadFromJsonAsync<JsonElement>(Web);
        var fresh = rotated.GetProperty("credential").GetString()!;
        Assert.NotEqual(oldCredential, fresh);
        Assert.StartsWith(deviceId.ToString("N") + ".", fresh);
        Assert.Equal(HttpStatusCode.Unauthorized, (await device.GetAsync("/api/v1/device/snapshot")).StatusCode);   // the old one is dead
        device.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fresh);
        Assert.Equal(HttpStatusCode.OK, (await device.GetAsync("/api/v1/device/snapshot")).StatusCode);
    }

    [Fact]
    public async Task Deleting_the_pet_account_data_removes_its_devices_and_pairing_tells_other_apps()
    {
        using var f = new AuthFactory();
        var (owner, _) = await Owner(f);
        var (device, _, _) = await Pair(f, owner);
        var types = (await owner.GetFromJsonAsync<JsonElement>("/api/v1/events", Web)).GetProperty("events").EnumerateArray().Select(e => e.GetProperty("type").GetString()).ToList();
        Assert.Contains("devices.updated", types);                                         // the app learns a device was approved and activated

        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Pets.RemoveRange(db.Pets);
            await db.SaveChangesAsync();
            Assert.False(await db.Devices.AnyAsync());                                      // cascade
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await device.GetAsync("/api/v1/device/snapshot")).StatusCode);
    }

    [Fact]
    public async Task The_model_name_is_checked()
    {
        using var f = new AuthFactory();
        var device = Bare(f);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await device.PostAsJsonAsync("/api/v1/device/pairing/start", new { model = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await device.PostAsJsonAsync("/api/v1/device/pairing/start", new { model = "<script>" })).StatusCode);
    }
}
