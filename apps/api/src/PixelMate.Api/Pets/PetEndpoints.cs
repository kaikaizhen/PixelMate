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
        g.MapGet("/me/events", GetEvents);
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

    private static Life.LifeOptions LifeOf(HttpContext ctx) => ctx.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Life.LifeOptions>>().Value;

    private static PetWorld World(Guid petId, Life.ActivityInfo a, DateTime now, Life.LifeOptions o, Weather.WeatherSnapshot? w = null, string? tz = null) =>
        new("happy", Life.WorldRules.SceneAt(petId, a.Key, Life.LifeScheduler.SlotOf(now)),
            w?.Condition ?? Life.WorldRules.ConditionAt(now, o.UtcOffsetHours), w?.TemperatureC, w?.Source ?? Weather.Sources.Simulated, tz ?? "Asia/Taipei");

    internal static PetSnapshot ToSnapshot(Pet p, DateTime? now = null, Life.LifeOptions? life = null, Weather.WeatherSnapshot? weather = null, string? timeZone = null)
    {
        var skills = SkillKeys.All.ToDictionary(k => k, k => p.Skills.FirstOrDefault(s => s.SkillKey == k)?.Points ?? 0);
        var t = now ?? DateTime.UtcNow;
        var activity = Life.LifeScheduler.ActivityNow(p.PetId, skills, t);
        var remaining = Math.Max(0, PetRules.TotalSkillPoints(p.Level) - skills.Values.Sum());
        return new PetSnapshot(p.PetId, p.Name, p.BodyVariant, p.Level, p.Xp, p.Revision, skills, remaining,
            EquipmentOf(p),
            World(p.PetId, activity, t, life ?? new Life.LifeOptions(), weather, timeZone), p.UpdatedAt, activity);
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
        return Results.Created("/api/v1/pets/me", ToSnapshot(pet, null, LifeOf(ctx), (await Weather.WeatherEndpoints.ForUser(owner, db, ctx.RequestServices.GetRequiredService<Weather.WeatherService>(), ct)).Weather));
    }

    /// <summary>Reading the pet also makes up any finished slots that the worker has not reached yet (lazy half of the hybrid).</summary>
    private static async Task<IResult> GetMine(ClaimsPrincipal principal, AppDbContext db, Life.LifeService life, TimeProvider clock, Microsoft.Extensions.Options.IOptions<Life.LifeOptions> lifeOptions, HttpContext ctx, CancellationToken ct)
    {
        var owner = UserId(principal);
        var now = clock.GetUtcNow().UtcDateTime;
        var pet = await WithEquipment(db.Pets.AsNoTracking()).FirstOrDefaultAsync(p => p.OwnerId == owner, ct);
        if (pet is null) return NoPet(ctx);
        await life.CatchUpAsync(db, pet.PetId, owner, pet.CreatedAt, now, ct);
        var (w, loc) = await Weather.WeatherEndpoints.ForUser(owner, db, ctx.RequestServices.GetRequiredService<Weather.WeatherService>(), ct);
        return Results.Ok(ToSnapshot(pet, now, lifeOptions.Value, w, loc?.TimeZone));
    }

    /// <summary>The latest things the pet found, newest first.</summary>
    private static async Task<IResult> GetEvents(ClaimsPrincipal principal, AppDbContext db, int? limit, HttpContext ctx, CancellationToken ct)
    {
        var owner = UserId(principal);
        var take = Math.Clamp(limit ?? 20, 1, 50);
        var petId = await db.Pets.AsNoTracking().Where(p => p.OwnerId == owner).Select(p => (Guid?)p.PetId).FirstOrDefaultAsync(ct);
        if (petId is null) return NoPet(ctx);
        var rows = await (from e in db.PetEvents.AsNoTracking()
                          join d in db.ItemDefinitions.AsNoTracking() on e.ItemId equals d.ItemId
                          where e.PetId == petId
                          orderby e.OccurredAt descending
                          select new Life.PetEventDto(e.EventId, e.Kind, d.ItemId, d.Name, d.AssetKey, d.Rarity, e.OccurredAt)).Take(take).ToListAsync(ct);
        return Results.Ok(new { events = rows });
    }

    /// <summary>All-or-nothing: points, cap and revision are checked, then skill + revision are written in one transaction.</summary>
    private static async Task<IResult> Allocate(AllocateSkillRequest req, ClaimsPrincipal principal, AppDbContext db, Realtime.EventOutbox outbox, HttpContext ctx, CancellationToken ct)
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
        outbox.Add(owner, Realtime.EventTypes.PetUpdated, new { reason = "skills" }, pet.PetId, pet.Revision);
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
    private static async Task<IResult> Reset(ResetSkillsRequest req, ClaimsPrincipal principal, AppDbContext db, Realtime.EventOutbox outbox, HttpContext ctx, CancellationToken ct)
    {
        var owner = UserId(principal);
        var pet = await WithEquipment(db.Pets).FirstOrDefaultAsync(p => p.OwnerId == owner, ct);
        if (pet is null) return NoPet(ctx);
        if (pet.Revision != req.ExpectedRevision) return Conflict(ctx);

        foreach (var s in pet.Skills) s.Points = 0;
        pet.Revision++;
        pet.UpdatedAt = DateTime.UtcNow;
        outbox.Add(owner, Realtime.EventTypes.PetUpdated, new { reason = "skills" }, pet.PetId, pet.Revision);
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
        api.MapPost("/dev/pets/me/level-up", async (ClaimsPrincipal principal, AppDbContext db, Realtime.EventOutbox outbox, HttpContext ctx, CancellationToken ct) =>
        {
            var owner = UserId(principal);
            var pet = await WithEquipment(db.Pets).FirstOrDefaultAsync(p => p.OwnerId == owner, ct);
            if (pet is null) return NoPet(ctx);
            pet.Level = Math.Min(pet.Level + 1, 30);
            pet.Revision++;
            pet.UpdatedAt = DateTime.UtcNow;
            outbox.Add(owner, Realtime.EventTypes.PetUpdated, new { reason = "level" }, pet.PetId, pet.Revision);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToSnapshot(pet, null, LifeOf(ctx), (await Weather.WeatherEndpoints.ForUser(owner, db, ctx.RequestServices.GetRequiredService<Weather.WeatherService>(), ct)).Weather));
        }).RequireAuthorization();
        // Moves the life cursor back so the next read makes those hours up again (deterministic: the same walks find the same items).
        api.MapPost("/dev/pets/me/life/rewind", async (int hours, ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct) =>
        {
            var owner = UserId(principal);
            var petId = await db.Pets.Where(p => p.OwnerId == owner).Select(p => (Guid?)p.PetId).FirstOrDefaultAsync(ct);
            if (petId is null) return NoPet(ctx);
            var life = await db.PetLives.FirstOrDefaultAsync(l => l.PetId == petId, ct);
            if (life is null) return Results.Ok(new { cursorSlot = (long?)null });
            life.CursorSlot -= Math.Clamp(hours, 1, 24 * 7) * 4;
            await db.PetEvents.Where(e => e.PetId == petId && e.Slot > life.CursorSlot).ExecuteDeleteAsync(ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { cursorSlot = (long?)life.CursorSlot });
        }).RequireAuthorization();
        return api;
    }
}
