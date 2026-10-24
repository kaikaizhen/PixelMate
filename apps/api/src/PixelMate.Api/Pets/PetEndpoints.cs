using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;

namespace PixelMate.Api.Pets;

public static class PetEndpoints
{
    public static RouteGroupBuilder MapPets(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/pets").RequireAuthorization();
        g.MapPost("/", Create).Validate<CreatePetRequest>();
        g.MapGet("/me", GetMine);
        return api;
    }

    private static Guid UserId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);

    internal static PetSnapshot ToSnapshot(Pet p)
    {
        var skills = SkillKeys.All.ToDictionary(k => k, k => p.Skills.FirstOrDefault(s => s.SkillKey == k)?.Points ?? 0);
        var remaining = Math.Max(0, PetRules.TotalSkillPoints(p.Level) - skills.Values.Sum());
        return new PetSnapshot(p.PetId, p.Name, p.BodyVariant, p.Level, p.Xp, p.Revision, skills, remaining,
            new Dictionary<string, string?> { ["hat"] = null, ["outfit"] = null },
            new PetWorld("happy", "home", "sunny"), p.UpdatedAt);
    }

    private static async Task<IResult> Create(CreatePetRequest req, ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var owner = UserId(principal);                      // never taken from the request body
        var conflict = Results.Json(new ApiError(ErrorCodes.PetAlreadyExists, "你已經有一隻小夥伴了。", ctx.TraceIdentifier), statusCode: 409);

        if (await db.Pets.AnyAsync(p => p.OwnerId == owner, ct))
        {
            return conflict;
        }

        var now = DateTime.UtcNow;
        var pet = new Pet { OwnerId = owner, Name = req.Name.Trim(), BodyVariant = req.BodyVariant, CreatedAt = now, UpdatedAt = now };
        pet.Skills.AddRange(SkillKeys.All.Select(k => new PetSkill { PetId = pet.PetId, SkillKey = k, Points = 0 }));
        db.Pets.Add(pet);
        try
        {
            await db.SaveChangesAsync(ct);                  // pet + 4 skills in one transaction
        }
        catch (DbUpdateException)
        {
            return conflict;                                // two tabs raced: the unique index on owner_id decides
        }
        return Results.Created("/api/v1/pets/me", ToSnapshot(pet));
    }

    private static async Task<IResult> GetMine(ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var owner = UserId(principal);
        var pet = await db.Pets.AsNoTracking().Include(p => p.Skills).FirstOrDefaultAsync(p => p.OwnerId == owner, ct);
        return pet is null
            ? Results.Json(new ApiError(ErrorCodes.NotFound, "你還沒有小夥伴。", ctx.TraceIdentifier), statusCode: 404)
            : Results.Ok(ToSnapshot(pet));
    }
}
