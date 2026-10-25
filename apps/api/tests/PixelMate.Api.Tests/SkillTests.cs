using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Pets;

namespace PixelMate.Api.Tests;

public class SkillTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task<(HttpClient C, PetSnapshot Pet)> PlayerWithPet(AuthFactory f, int level = 1, string email = "a@example.com")
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email);
        await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" });
        var pet = (await (await c.PostAsJsonAsync("/api/v1/pets", new { name = "小栗", bodyVariant = "star_01_cream" })).Content.ReadFromJsonAsync<PetSnapshot>(Web))!;
        if (level > 1) await SetLevel(f, pet.PetId, level);
        return (c, (await c.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!);
    }

    private static async Task SetLevel(AuthFactory f, Guid petId, int level)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var p = await db.Pets.FirstAsync(x => x.PetId == petId);
        p.Level = level;
        p.Revision++;                      // what any real level-up would do
        await db.SaveChangesAsync();
    }

    private static Task<HttpResponseMessage> Allocate(HttpClient c, string key, int points, int rev) =>
        c.PostAsJsonAsync("/api/v1/pets/me/skills/allocate", new { skillKey = key, pointsToAdd = points, expectedRevision = rev });

    private static async Task<ApiError> Err(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<ApiError>(Web))!;

    [Fact]
    public async Task Skill_endpoints_need_a_session_and_a_pet()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Allocate(c, "empathy", 1, 1)).StatusCode);

        await AuthFactory.Register(c);
        await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a@example.com", password = "Passw0rd-123" });
        var res = await Allocate(c, "empathy", 1, 1);                          // signed in, but no pet yet
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Level_1_has_no_points_to_spend()
    {
        using var f = new AuthFactory();
        var (c, pet) = await PlayerWithPet(f);
        var res = await Allocate(c, "empathy", 1, pet.Revision);
        Assert.Equal((HttpStatusCode)422, res.StatusCode);
        Assert.Equal(ErrorCodes.InsufficientPoints, (await Err(res)).Code);
    }

    [Fact]
    public async Task Allocating_updates_skills_remaining_and_revision_and_survives_a_reload()
    {
        using var f = new AuthFactory();
        var (c, pet) = await PlayerWithPet(f, level: 4);                       // 3 points
        Assert.Equal(3, pet.RemainingSkillPoints);

        var res = await Allocate(c, "empathy", 2, pet.Revision);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var r = (await res.Content.ReadFromJsonAsync<SkillsResult>(Web))!;
        Assert.Equal(pet.Revision + 1, r.Revision);
        Assert.Equal(2, r.Skills["empathy"]);
        Assert.Equal(1, r.RemainingSkillPoints);

        var again = (await c.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!;
        Assert.Equal(2, again.Skills["empathy"]);
        Assert.Equal(r.Revision, again.Revision);
    }

    [Fact]
    public async Task Spending_more_than_you_have_changes_nothing()
    {
        using var f = new AuthFactory();
        var (c, pet) = await PlayerWithPet(f, level: 3);                       // 2 points
        var res = await Allocate(c, "wisdom", 3, pet.Revision);
        Assert.Equal((HttpStatusCode)422, res.StatusCode);
        Assert.Equal(ErrorCodes.InsufficientPoints, (await Err(res)).Code);
        var after = (await c.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!;
        Assert.All(after.Skills.Values, v => Assert.Equal(0, v));
        Assert.Equal(pet.Revision, after.Revision);                            // no partial write, no revision bump
    }

    [Fact]
    public async Task One_skill_is_capped_at_5_even_with_points_left()
    {
        using var f = new AuthFactory();
        var (c, pet) = await PlayerWithPet(f, level: 10);                      // 9 points
        var first = (await (await Allocate(c, "creativity", 5, pet.Revision)).Content.ReadFromJsonAsync<SkillsResult>(Web))!;
        var res = await Allocate(c, "creativity", 1, first.Revision);
        Assert.Equal((HttpStatusCode)422, res.StatusCode);
        Assert.Contains("5", (await Err(res)).Message);
        Assert.Equal(5, (await c.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!.Skills["creativity"]);
    }

    [Theory]
    [InlineData("charisma", 1, 1)]
    [InlineData("empathy", 0, 1)]
    [InlineData("empathy", -2, 1)]
    [InlineData("empathy", 6, 1)]
    [InlineData("empathy", 1, 0)]
    public async Task Bad_requests_are_422(string key, int points, int rev)
    {
        using var f = new AuthFactory();
        var (c, _) = await PlayerWithPet(f, level: 5);
        var res = await Allocate(c, key, points, rev);
        Assert.Equal((HttpStatusCode)422, res.StatusCode);
        Assert.Equal(ErrorCodes.ValidationFailed, (await Err(res)).Code);
    }

    [Fact]
    public async Task A_stale_revision_is_a_409_and_a_double_tap_cannot_spend_twice()
    {
        using var f = new AuthFactory();
        var (c, pet) = await PlayerWithPet(f, level: 4);
        var ok = await Allocate(c, "empathy", 1, pet.Revision);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var replay = await Allocate(c, "empathy", 1, pet.Revision);            // the same request again (retry / second device)
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
        Assert.Equal(ErrorCodes.RevisionConflict, (await Err(replay)).Code);
        Assert.Equal(1, (await c.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!.Skills["empathy"]);
    }

    [Fact]
    public async Task The_database_rejects_a_write_made_from_an_outdated_revision()
    {
        // The endpoint checks the revision, but two requests can both pass that check: the concurrency token decides.
        using var f = new AuthFactory();
        var (_, pet) = await PlayerWithPet(f, level: 4);
        using var a = f.Services.CreateScope();
        using var b = f.Services.CreateScope();
        var dbA = a.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbB = b.ServiceProvider.GetRequiredService<AppDbContext>();
        var petA = await dbA.Pets.Include(p => p.Skills).FirstAsync(p => p.PetId == pet.PetId);
        var petB = await dbB.Pets.Include(p => p.Skills).FirstAsync(p => p.PetId == pet.PetId);

        petA.Skills.First().Points = 1; petA.Revision++;
        await dbA.SaveChangesAsync();
        petB.Skills.First().Points = 2; petB.Revision++;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }

    [Fact]
    public async Task Reset_gives_every_point_back_and_is_also_revision_checked()
    {
        using var f = new AuthFactory();
        var (c, pet) = await PlayerWithPet(f, level: 6);                       // 5 points
        var spent = (await (await Allocate(c, "empathy", 3, pet.Revision)).Content.ReadFromJsonAsync<SkillsResult>(Web))!;
        Assert.Equal(2, spent.RemainingSkillPoints);

        var stale = await c.PostAsJsonAsync("/api/v1/pets/me/skills/reset", new { expectedRevision = pet.Revision });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var reset = await c.PostAsJsonAsync("/api/v1/pets/me/skills/reset", new { expectedRevision = spent.Revision });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        var r = (await reset.Content.ReadFromJsonAsync<SkillsResult>(Web))!;
        Assert.All(r.Skills.Values, v => Assert.Equal(0, v));
        Assert.Equal(5, r.RemainingSkillPoints);
        Assert.Equal(spent.Revision + 1, r.Revision);
    }

    [Fact]
    public async Task One_player_cannot_touch_anothers_skills()
    {
        using var f = new AuthFactory();
        var (a, aPet) = await PlayerWithPet(f, level: 4, "a@example.com");
        var (b, bPet) = await PlayerWithPet(f, level: 4, "b@example.com");
        await Allocate(b, "wisdom", 3, bPet.Revision);                         // B spends everything
        var aNow = (await a.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!;
        Assert.All(aNow.Skills.Values, v => Assert.Equal(0, v));               // A is untouched
        Assert.Equal(aPet.Revision, aNow.Revision);
        Assert.Equal(3, aNow.RemainingSkillPoints);
    }
}
