using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PixelMate.Api.Data;
using PixelMate.Api.Life;
using PixelMate.Api.Pets;

namespace PixelMate.Api.Tests;

public class LifeTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, int> NoSkills = new Dictionary<string, int>();
    private static IReadOnlyDictionary<string, int> Skills(string key, int points) => new Dictionary<string, int> { [key] = points };
    private static readonly LifeOptions Rules = new();

    private static async Task<(HttpClient C, PetSnapshot Pet)> Player(AuthFactory f, string email = "a@example.com")
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email);
        await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" });
        var pet = (await (await c.PostAsJsonAsync("/api/v1/pets", new { name = "小栗", bodyVariant = "star_01_cream" })).Content.ReadFromJsonAsync<PetSnapshot>(Web))!;
        return (c, pet);
    }

    private static async Task<PetSnapshot> Get(HttpClient c) => (await c.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!;

    private static async Task<List<PetEvent>> Events(AuthFactory f, Guid petId)
    {
        using var scope = f.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().PetEvents.AsNoTracking().Where(e => e.PetId == petId).OrderBy(e => e.Slot).ToListAsync();
    }

    private static async Task<int> BagTotal(AuthFactory f, Guid petId)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owner = (await db.Pets.AsNoTracking().FirstAsync(p => p.PetId == petId)).OwnerId;
        return await db.InventoryEntries.Where(i => i.OwnerId == owner).SumAsync(i => i.Quantity);
    }

    // ---- pure rules -------------------------------------------------------------------------------------------------
    [Fact]
    public void The_activity_of_a_slot_is_always_the_same_answer()
    {
        var pet = Guid.NewGuid();
        for (long slot = 1_000; slot < 1_300; slot++)
        {
            Assert.Equal(LifeScheduler.ActivityAt(pet, NoSkills, slot), LifeScheduler.ActivityAt(pet, NoSkills, slot));
        }
        Assert.Equal(96, LifeScheduler.SlotsPerDay);
        Assert.Equal(LifeScheduler.StartOf(5).AddMinutes(15), LifeScheduler.StartOf(6));
        Assert.Equal(5, LifeScheduler.SlotOf(LifeScheduler.StartOf(5).AddMinutes(14)));
    }

    [Theory]
    [InlineData("exploration", Activities.Walk)]
    [InlineData("wisdom", Activities.Read)]
    [InlineData("creativity", Activities.Hum)]
    [InlineData("empathy", Activities.Letter)]
    public void Skill_points_make_their_own_activity_more_likely(string skill, string activity)
    {
        int Count(IReadOnlyDictionary<string, int> s) =>
            Enumerable.Range(0, 400).Sum(i => Enumerable.Range(0, 96).Count(slot => LifeScheduler.ActivityAt(new Guid(i, 0, 0, new byte[8]), s, slot) == activity));
        Assert.True(Count(Skills(skill, 5)) > Count(NoSkills) * 1.5);
    }

    [Fact]
    public void A_pet_without_points_mostly_idles_and_every_activity_still_happens()
    {
        var all = Enumerable.Range(0, 5000).Select(s => LifeScheduler.ActivityAt(Guid.Parse("11111111-1111-1111-1111-111111111111"), NoSkills, s)).ToList();
        Assert.Equal(Activities.All.Order(), all.Distinct().Order());
        Assert.InRange(all.Count(a => a == Activities.Idle) / 5000.0, 0.5, 0.7);
    }

    /// <summary>Finds per day with the same rules the service applies (walks only, at most MaxFindsPerDay).</summary>
    private static double FindsPerDay(IReadOnlyDictionary<string, int> skills, int pets = 300, int days = 20, int maxPerDay = 3, Action<int>? each = null)
    {
        var total = 0;
        for (var p = 0; p < pets; p++)
        {
            var id = new Guid(p + 1, 0, 0, new byte[8]);
            for (var d = 0; d < days; d++)
            {
                var n = 0;
                for (var s = 0; s < 96 && n < maxPerDay; s++)
                {
                    if (LifeScheduler.FindsSomething(id, skills, 100_000 + d * 96 + s, Rules)) n++;
                }
                each?.Invoke(n);
                total += n;
            }
        }
        return total / (double)(pets * days);
    }

    [Fact]
    public void Finds_average_about_two_a_day_never_more_than_three_and_exploration_makes_them_likelier()
    {
        var max = 0;
        var none = FindsPerDay(NoSkills, each: n => max = Math.Max(max, n));
        var some = FindsPerDay(Skills("exploration", 3));
        var lots = FindsPerDay(Skills("exploration", 5));
        Assert.InRange(none, 1.5, 2.5);
        Assert.True(some > none && lots > some, $"{none} {some} {lots}");
        Assert.InRange(lots, 2.4, 3.0);
        Assert.Equal(3, max);
    }

    [Fact]
    public void Items_are_picked_by_rarity_and_exploration_shifts_the_odds()
    {
        var catalogue = Enumerable.Range(0, 60).Select(i => ($"c{i}", "common")).Concat(Enumerable.Range(0, 20).Select(i => ($"r{i}", "rare")))
            .Concat(Enumerable.Range(0, 5).Select(i => ($"p{i}", "precious"))).ToList();
        double Rare(IReadOnlyDictionary<string, int> s) =>
            Enumerable.Range(0, 4000).Count(i => LifeScheduler.PickItem(Guid.Parse("22222222-2222-2222-2222-222222222222"), s, i, catalogue)[0] != 'c') / 4000.0;
        Assert.True(Rare(Skills("exploration", 5)) > Rare(NoSkills) + 0.1);
        Assert.Equal(LifeScheduler.PickItem(Guid.Empty, NoSkills, 7, catalogue), LifeScheduler.PickItem(Guid.Empty, NoSkills, 7, catalogue));
    }

    // ---- the world ----------------------------------------------------------------------------------------------------
    [Fact]
    public void Weather_is_shared_follows_the_time_of_day_and_only_uses_art_that_exists()
    {
        var start = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var seen = new HashSet<string>();
        for (var h = 0; h < 24 * 400; h++) seen.Add(WorldRules.WeatherAt(start.AddHours(h), 8));
        Assert.All(seen, k => Assert.Contains(k, WorldRules.WeatherKeys));
        Assert.True(seen.Count >= 10, string.Join(",", seen));
        Assert.Equal(WorldRules.WeatherAt(start, 8), WorldRules.WeatherAt(start, 8));

        // a fair-weather July day, scanned for clear blocks: night is starry and early morning is sunrise
        var clearNight = Enumerable.Range(0, 200).Select(d => start.AddDays(d).AddHours(-8 + 2)).Select(t => WorldRules.WeatherAt(t, 8)).ToList();
        Assert.Contains("starry", clearNight);
        Assert.DoesNotContain("snow", Enumerable.Range(0, 24 * 90).Select(h => WorldRules.WeatherAt(start.AddHours(h), 8)));   // no snow in summer
    }

    [Fact]
    public void Scenes_are_stable_cover_every_place_and_walks_favour_the_outdoors()
    {
        var pet = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Assert.Equal(WorldRules.SceneAt(pet, Activities.Read, 123), WorldRules.SceneAt(pet, Activities.Read, 123));
        double Outdoors(string a) => Enumerable.Range(0, 3000).Count(s => WorldRules.SceneAt(pet, a, s) != Scenes.Home) / 3000.0;
        Assert.True(Outdoors(Activities.Walk) > Outdoors(Activities.Read) && Outdoors(Activities.Read) > Outdoors(Activities.Idle));
        Assert.Equal(Scenes.All.Order(), Enumerable.Range(0, 3000).Select(s => WorldRules.SceneAt(pet, Activities.Read, s)).Distinct().Order());
    }

    [Fact]
    public async Task The_snapshot_says_where_the_pet_is_and_what_the_weather_is()
    {
        using var f = new AuthFactory();
        var (c, pet) = await Player(f);
        var now = f.Clock.GetUtcNow().UtcDateTime;
        var snap = await Get(c);
        Assert.Equal("clear", snap.World.Weather);                                   // the (fake) Open-Meteo says clear: the device adds sunrise/day/sunset/night itself
        Assert.Equal("open-meteo", snap.World.WeatherSource);
        Assert.Equal(WorldRules.SceneAt(pet.PetId, snap.Activity.Key, LifeScheduler.SlotOf(now)), snap.World.Scene);
        Assert.Contains(snap.World.Scene, Scenes.All);
    }

    // ---- through the API --------------------------------------------------------------------------------------------
    [Fact]
    public async Task The_snapshot_carries_the_activity_every_device_must_show()
    {
        using var f = new AuthFactory();
        var (c, pet) = await Player(f);
        var now = f.Clock.GetUtcNow().UtcDateTime;
        var a = (await Get(c)).Activity;
        Assert.Contains(a.Key, Activities.All);
        Assert.Equal(LifeScheduler.ActivityAt(pet.PetId, NoSkills, LifeScheduler.SlotOf(now)), a.Key);
        Assert.Equal(TimeSpan.FromMinutes(15), a.EndsAt - a.StartedAt);
        Assert.True(a.StartedAt <= now && now < a.EndsAt);

        f.Clock.Advance(TimeSpan.FromMinutes(15));      // the next slot starts: a different (but just as shared) answer
        var b = (await Get(c)).Activity;
        Assert.Equal(a.EndsAt, b.StartedAt);
    }

    [Fact]
    public async Task Coming_back_after_days_finds_items_in_the_bag_within_the_daily_limit()
    {
        using var f = new AuthFactory();
        var (c, pet) = await Player(f);
        Assert.Empty(await Events(f, pet.PetId));

        f.Clock.Advance(TimeSpan.FromDays(10));
        await Get(c);
        var events = await Events(f, pet.PetId);
        Assert.NotEmpty(events);
        Assert.All(events.GroupBy(e => LifeScheduler.DayOf(e.Slot)), g => Assert.True(g.Count() <= 3));
        Assert.Equal(events.Count, await BagTotal(f, pet.PetId));

        var again = await Events(f, pet.PetId);
        await Get(c); await Get(c);                      // reading again changes nothing
        Assert.Equal(again.Count, (await Events(f, pet.PetId)).Count);
        Assert.Equal(events.Count, await BagTotal(f, pet.PetId));

        var list = (await c.GetFromJsonAsync<JsonElement>("/api/v1/pets/me/events?limit=50", Web)).GetProperty("events");
        Assert.Equal(Math.Min(events.Count, 50), list.GetArrayLength());
        Assert.True(list[0].GetProperty("name").GetString()!.Length > 0);
    }

    [Fact]
    public async Task The_worker_makes_up_the_days_even_when_no_device_asked_and_a_second_pass_adds_nothing()
    {
        using var f = new AuthFactory();
        var (_, pet) = await Player(f);
        f.Clock.Advance(TimeSpan.FromDays(3));
        var worker = f.Services.GetServices<IHostedService>().OfType<LifeWorker>().Single();

        var first = await worker.RunOnce(default);
        Assert.Equal(first, (await Events(f, pet.PetId)).Count);
        Assert.Equal(first, await BagTotal(f, pet.PetId));
        Assert.Equal(0, await worker.RunOnce(default));
        Assert.Equal(first, await BagTotal(f, pet.PetId));
    }

    [Fact]
    public async Task The_api_and_the_worker_working_at_once_still_give_every_slot_one_event_and_one_grant()
    {
        using var f = new AuthFactory();
        var (c, pet) = await Player(f);
        f.Clock.Advance(TimeSpan.FromDays(6));
        var worker = f.Services.GetServices<IHostedService>().OfType<LifeWorker>().Single();

        // the test database is one shared in-memory SQLite connection, so a racing call may fail outright; what must hold is the end state
        static async Task Quietly(Func<Task> run) { try { await run(); } catch (Exception) { /* a lost race */ } }
        await Task.WhenAll(Enumerable.Range(0, 4).SelectMany(_ => new[] { Quietly(() => worker.RunOnce(default)), Quietly(() => Get(c)) }));
        await worker.RunOnce(default);                      // a calm pass finishes whatever the race left behind
        var events = await Events(f, pet.PetId);
        Assert.Equal(events.Count, events.Select(e => e.Slot).Distinct().Count());
        Assert.Equal(events.Count, await BagTotal(f, pet.PetId));
        Assert.All(events.GroupBy(e => LifeScheduler.DayOf(e.Slot)), g => Assert.True(g.Count() <= 3));
    }

    [Fact]
    public async Task A_month_offline_only_makes_up_the_last_week()
    {
        using var f = new AuthFactory();
        var (c, pet) = await Player(f);
        f.Clock.Advance(TimeSpan.FromDays(30));
        await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" });   // the old cookie has expired by now
        await Get(c);
        var events = await Events(f, pet.PetId);
        var now = f.Clock.GetUtcNow().UtcDateTime;
        Assert.All(events, e => Assert.True(e.OccurredAt >= now.AddDays(-7).AddMinutes(-15)));
        Assert.True(events.Count <= 7 * 3 + 3);
    }

    [Fact]
    public async Task Pets_only_see_their_own_events()
    {
        using var f = new AuthFactory();
        var (a, petA) = await Player(f, "a@example.com");
        var (b, _) = await Player(f, "b@example.com");
        f.Clock.Advance(TimeSpan.FromDays(10));
        await Get(a);
        var listB = (await b.GetFromJsonAsync<JsonElement>("/api/v1/pets/me/events", Web)).GetProperty("events");
        Assert.Equal(0, listB.GetArrayLength());
        Assert.NotEmpty(await Events(f, petA.PetId));
    }
}
