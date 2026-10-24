using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Pets;

namespace PixelMate.Api.Tests;

public class PetTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static async Task<HttpClient> SignedIn(AuthFactory f, string email = "a@example.com")
    {
        var c = f.NewClient();
        await AuthFactory.Register(c, email);
        var res = await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd-123" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return c;
    }

    private static Task<HttpResponseMessage> Adopt(HttpClient c, string name = "小栗", string variant = "star_01_cream") =>
        c.PostAsJsonAsync("/api/v1/pets", new { name, bodyVariant = variant });

    [Fact]
    public async Task Pet_endpoints_need_a_session()
    {
        using var f = new AuthFactory();
        var c = f.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/pets/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Adopt(c)).StatusCode);
    }

    [Fact]
    public async Task No_pet_yet_is_a_404_then_adopting_returns_the_contract_snapshot()
    {
        using var f = new AuthFactory();
        var c = await SignedIn(f);

        var none = await c.GetAsync("/api/v1/pets/me");
        Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
        Assert.Equal(ErrorCodes.NotFound, (await none.Content.ReadFromJsonAsync<ApiError>(Web))!.Code);

        var created = await Adopt(c, " 小栗 ");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var snap = (await created.Content.ReadFromJsonAsync<PetSnapshot>(Web))!;
        Assert.Equal("小栗", snap.Name);                       // trimmed
        Assert.Equal("star_01_cream", snap.BodyVariant);
        Assert.Equal((1, 0, 1), (snap.Level, snap.Xp, snap.Revision));
        Assert.Equal(new[] { "creativity", "empathy", "exploration", "wisdom" }, snap.Skills.Keys.Order().ToArray());
        Assert.All(snap.Skills.Values, v => Assert.Equal(0, v));
        Assert.Equal(0, snap.RemainingSkillPoints);
        Assert.Equal("home", snap.World.Scene);

        var mine = (await c.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!;
        Assert.Equal(snap.PetId, mine.PetId);
    }

    [Fact]
    public async Task A_second_pet_is_refused_with_409()
    {
        using var f = new AuthFactory();
        var c = await SignedIn(f);
        await Adopt(c);
        var again = await Adopt(c, "另一隻", "star_01_mint");
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(ErrorCodes.PetAlreadyExists, (await again.Content.ReadFromJsonAsync<ApiError>(Web))!.Code);
    }

    [Fact]
    public async Task The_database_itself_allows_only_one_pet_per_owner()
    {
        // The endpoint pre-checks, but two tabs can race past that check: the unique index on owner_id must still say no.
        using var f = new AuthFactory();
        var c = await SignedIn(f);
        var snap = (await (await Adopt(c)).Content.ReadFromJsonAsync<PetSnapshot>(Web))!;
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owner = (await db.Pets.AsNoTracking().FirstAsync(p => p.PetId == snap.PetId)).OwnerId;
        db.Pets.Add(new Pet { OwnerId = owner, Name = "第二隻", BodyVariant = "star_01_mint" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Players_only_ever_see_their_own_pet_and_the_owner_cannot_be_chosen()
    {
        using var f = new AuthFactory();
        var a = await SignedIn(f, "a@example.com");
        var b = await SignedIn(f, "b@example.com");
        var aPet = await (await Adopt(a, "甲")).Content.ReadFromJsonAsync<PetSnapshot>(Web);

        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync("/api/v1/pets/me")).StatusCode);
        var meId = (await a.GetFromJsonAsync<JsonElement>("/api/v1/auth/me")).GetProperty("userId").GetString();
        var bAdopt = await b.PostAsJsonAsync("/api/v1/pets", new { name = "乙", bodyVariant = "star_01_mint", ownerId = meId });   // extra field is ignored
        Assert.Equal(HttpStatusCode.Created, bAdopt.StatusCode);
        var bPet = await b.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web);
        Assert.NotEqual(aPet!.PetId, bPet!.PetId);
        Assert.Equal("甲", (await a.GetFromJsonAsync<PetSnapshot>("/api/v1/pets/me", Web))!.Name);
    }

    [Theory]
    [InlineData("", "star_01_cream", "name")]
    [InlineData("   ", "star_01_cream", "name")]
    [InlineData("一二三四五六七八九十一二三", "star_01_cream", "name")]
    [InlineData("<b>小栗</b>", "star_01_cream", "name")]
    [InlineData("小栗😀", "star_01_cream", "name")]
    [InlineData("小栗", "dragon_99", "bodyVariant")]
    public async Task Bad_names_and_unknown_looks_are_422_and_nothing_is_saved(string name, string variant, string field)
    {
        using var f = new AuthFactory();
        var c = await SignedIn(f);
        var res = await Adopt(c, name, variant);
        Assert.Equal((HttpStatusCode)422, res.StatusCode);
        Assert.True((await res.Content.ReadFromJsonAsync<ApiError>(Web))!.Errors!.ContainsKey(field));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/v1/pets/me")).StatusCode);
    }

    [Theory]
    [InlineData("小栗")]
    [InlineData("Mochi 2")]
    [InlineData("星・星")]
    [InlineData("星·星")]
    [InlineData("一二三四五六七八九十一二")]
    public async Task Reasonable_names_are_accepted(string name)
    {
        using var f = new AuthFactory();
        Assert.Equal(HttpStatusCode.Created, (await Adopt(await SignedIn(f), name)).StatusCode);
    }

    [Fact]
    public async Task Skill_points_are_limited_to_0_to_5_by_the_database_itself()
    {
        using var f = new AuthFactory();
        var c = await SignedIn(f);
        var snap = (await (await Adopt(c)).Content.ReadFromJsonAsync<PetSnapshot>(Web))!;
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var skill = await db.PetSkills.FirstAsync(s => s.PetId == snap.PetId);
        skill.Points = 6;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
