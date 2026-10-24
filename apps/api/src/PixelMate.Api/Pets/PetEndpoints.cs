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
        g.MapPost("/me/skills/allocate", Allocate).Validate<AllocateSkillRequest>();
        g.MapPost("/me/skills/reset", Reset).Validate<ResetSkillsRequest>();
        return api;
    }

    private static IResult Conflict(HttpContext ctx) =>
        Results.Json(new ApiError(ErrorCodes.RevisionConflict, "小夥伴的資料已經在別的地方更新了，請重新整理後再試。", ctx.TraceIdentifier), statusCode: 409);

    private static IResult NoPet(HttpContext ctx) =>
        Results.Json(new ApiError(ErrorCodes.NotFound, "你還沒有小夥伴。", ctx.TraceIdentifier), statusCode: 404);

    private static SkillsResult ToResult(Pet p)
    {
        var snap = ToSnapshot(p);
        return new SkillsResult(p.Revision, snap.Skills, snap.RemainingSkillPoints);
    }

    private static Guid UserId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Slot -> itemId. Needs the inventory rows, so callers load p.Equipment with <see cref="WithEquipment"/>.</summary>
    private static IDictionary<string, string?> EquipmentOf(Pet p) =>
        Items.Slots.Wearable.ToDictionary(s => s, s => p.Equipment.FirstOrDefault(x => x.Slot == s)?.ItemId);

    internal static IQueryable<Pet> WithEquipment(IQueryable<Pet> q) => q.Include(p => p.Skills).Include(p => p.Equipment);

    internal static PetSnapshot ToSnapshot(Pet p)
    {
        var skills = SkillKeys.All.ToDictionary(k => k, k => p.Skills.FirstOrDefault(s => s.SkillKey == k)?.Points ?? 0);
        var remaining = Math.Max(0, PetRules.TotalSkillPoints(p.Level) - skills.Values.Sum());
        return new PetSnapshot(p.PetId, p.Name, p.BodyVariant, p.Level, p.Xp, p.Revision, skills, remaining,
            EquipmentOf(p),
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
        var pet = await WithEquipment(db.Pets.AsNoTracking()).FirstOrDefaultAsync(p => p.OwnerId == owner, ct);
        return pet is null
            ? Results.Json(new ApiError(ErrorCodes.NotFound, "你還沒有小夥伴。", ctx.TraceIdentifier), statusCode: 404)
            : Results.Ok(ToSnapshot(pet));
    }

    /// <summary>All-or-nothing: points, cap and revision are checked, then skill + revision are written in one transaction.</summary>
    private static async Task<IResult> Allocate(AllocateSkillRequest req, ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var owner = UserId(principal);
        var pet = await WithEquipment(db.Pets).FirstOrDefaultAsync(p => p.OwnerId == owner, ct);
        if (pet is null) return NoPet(ctx);
        if (pet.Revision != req.ExpectedRevision) return Conflict(ctx);

        var skill = pet.Skills.First(s => s.SkillKey == req.SkillKey);
        var remaining = PetRules.TotalSkillPoints(pet.Level) - pet.Skills.Sum(s => s.Points);
        if (req.PointsToAdd > remaining)
        {
            return Results.Json(new ApiError(ErrorCodes.InsufficientPoints, remaining <= 0 ? "沒有可以分配的技能點數。" : $"只剩 {remaining} 點可以分配。", ctx.TraceIdentifier), statusCode: 422);
        }
        if (skill.Points + req.PointsToAdd > SkillKeys.MaxPointsPerSkill)
        {
            return Results.Json(new ApiError(ErrorCodes.InsufficientPoints, $"每項技能最多 {SkillKeys.MaxPointsPerSkill} 點。", ctx.TraceIdentifier), statusCode: 422);
        }

        skill.Points += req.PointsToAdd;
        pet.Revision++;
        pet.UpdatedAt = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ctx);                           // another request moved the revision between our read and write
        }
        return Results.Ok(ToResult(pet));
    }

    /// <summary>Free reset (MVP): every skill back to 0, all points become spendable again.</summary>
    private static async Task<IResult> Reset(ResetSkillsRequest req, ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var owner = UserId(principal);
        var pet = await WithEquipment(db.Pets).FirstOrDefaultAsync(p => p.OwnerId == owner, ct);
        if (pet is null) return NoPet(ctx);
        if (pet.Revision != req.ExpectedRevision) return Conflict(ctx);

        foreach (var s in pet.Skills) s.Points = 0;
        pet.Revision++;
        pet.UpdatedAt = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ctx);
        }
        return Results.Ok(ToResult(pet));
    }

    /// <summary>Development only (not mapped in any other environment): gives levels so skill points can be tested before XP exists.</summary>
    internal static RouteGroupBuilder MapDev(this RouteGroupBuilder api)
    {
        api.MapPost("/dev/pets/me/level-up", async (ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var owner = UserId(principal);
            var pet = await WithEquipment(db.Pets).FirstOrDefaultAsync(p => p.OwnerId == owner, ct);
            if (pet is null) return NoPet(ctx);
            pet.Level = Math.Min(pet.Level + 1, 30);
            pet.Revision++;
            pet.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToSnapshot(pet));
        }).RequireAuthorization();
        return api;
    }
}
