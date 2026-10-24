using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;

namespace PixelMate.Api.Social;

public static class PairingEndpoints
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";   // no 0/O/1/I/L

    public static RouteGroupBuilder MapPairing(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/pairing").RequireAuthorization();
        g.MapPost("/invites", CreateInvite).Validate<CreateInviteRequest>();
        g.MapGet("/invites", ListInvites);
        g.MapDelete("/invites/{id:guid}", RevokeInvite);
        g.MapPost("/preview", Preview).Validate<CodeRequest>().RequireRateLimiting("pairing");
        g.MapPost("/accept", Accept).Validate<CodeRequest>().RequireRateLimiting("pairing");
        g.MapGet("/relationships", ListRelationships);
        g.MapDelete("/relationships/{id:guid}", EndRelationship);
        return api;
    }

    internal static Guid UserId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
    internal static IResult Err(HttpContext ctx, int status, string code, string message) => Results.Json(new ApiError(code, message, ctx.TraceIdentifier), statusCode: status);

    internal static (Guid Low, Guid High) Ordered(Guid a, Guid b) => a.CompareTo(b) < 0 ? (a, b) : (b, a);

    private static string Normalize(string code) => new string(code.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    private static string Hash(string normalized) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));

    private static string NewCode()
    {
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++) chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars, 0, 4) + "-" + new string(chars, 4, 4);
    }

    /// <summary>Active relationships of a user (ended ones do not count against the limits).</summary>
    internal static IQueryable<Relationship> ActiveOf(AppDbContext db, Guid user) =>
        db.Relationships.Where(r => r.EndedAt == null && (r.UserLowId == user || r.UserHighId == user));

    private static async Task<IResult> CreateInvite(CreateInviteRequest req, ClaimsPrincipal principal, AppDbContext db, IOptions<LetterOptions> options, TimeProvider clock, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var o = options.Value;
        var now = clock.GetUtcNow().UtcDateTime;
        if (await db.PairingInvites.CountAsync(i => i.InviterId == me && i.UsedAt == null && i.ExpiresAt > now, ct) >= o.MaxActiveInvites)
            return Err(ctx, 422, ErrorCodes.InvalidState, $"同時最多只能有 {o.MaxActiveInvites} 組邀請碼，請先收回不用的。");
        if (req.Kind == RelationKinds.Couple && await ActiveOf(db, me).AnyAsync(r => r.Kind == RelationKinds.Couple, ct))
            return Err(ctx, 422, ErrorCodes.InvalidState, "你已經有一位情侶了。");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = NewCode();
            var invite = new PairingInvite { InviterId = me, CodeHash = Hash(Normalize(code)), Kind = req.Kind, CreatedAt = now, ExpiresAt = now.AddHours(o.InviteHours) };
            db.PairingInvites.Add(invite);
            try
            {
                await db.SaveChangesAsync(ct);
                return Results.Created($"/api/v1/pairing/invites/{invite.InviteId}", new InviteCreated(invite.InviteId, code, invite.Kind, invite.ExpiresAt));   // the only time the code is shown
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();                            // the (very unlikely) same hash exists: draw another code
            }
        }
        return Err(ctx, 503, ErrorCodes.ServiceUnavailable, "暫時無法產生邀請碼，請再試一次。");
    }

    private static async Task<IResult> ListInvites(ClaimsPrincipal principal, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var me = UserId(principal);
        var now = clock.GetUtcNow().UtcDateTime;
        var rows = await db.PairingInvites.AsNoTracking().Where(i => i.InviterId == me && i.UsedAt == null && i.ExpiresAt > now)
            .OrderByDescending(i => i.CreatedAt).Select(i => new InviteDto(i.InviteId, i.Kind, i.CreatedAt, i.ExpiresAt)).ToListAsync(ct);
        return Results.Ok(new { invites = rows });
    }

    private static async Task<IResult> RevokeInvite(Guid id, ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var n = await db.PairingInvites.Where(i => i.InviteId == id && i.InviterId == me && i.UsedAt == null).ExecuteDeleteAsync(ct);
        return n == 0 ? Err(ctx, 404, ErrorCodes.NotFound, "找不到這組邀請碼。") : Results.NoContent();
    }

    private static IResult NoSuchCode(HttpContext ctx) => Err(ctx, 404, ErrorCodes.NotFound, "找不到這組邀請碼，或已經過期了。");

    private static Task<PairingInvite?> Find(AppDbContext db, string code, DateTime now, CancellationToken ct)
    {
        var hash = Hash(Normalize(code));
        return db.PairingInvites.FirstOrDefaultAsync(i => i.CodeHash == hash && i.UsedAt == null && i.ExpiresAt > now, ct);
    }

    private static async Task<IResult> Preview(CodeRequest req, ClaimsPrincipal principal, AppDbContext db, TimeProvider clock, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var invite = await Find(db, req.Code, clock.GetUtcNow().UtcDateTime, ct);
        if (invite is null) return NoSuchCode(ctx);
        if (invite.InviterId == me) return Err(ctx, 422, ErrorCodes.InvalidState, "這是你自己的邀請碼，要交給朋友喔。");
        var inviter = await db.Users.AsNoTracking().Where(u => u.Id == invite.InviterId).Select(u => u.DisplayName).FirstAsync(ct);
        var pet = await db.Pets.AsNoTracking().Where(p => p.OwnerId == invite.InviterId).Select(p => p.Name).FirstOrDefaultAsync(ct);
        return Results.Ok(new InvitePreview(inviter, pet, invite.Kind, invite.ExpiresAt));
    }

    /// <summary>The second confirmation: the code's owner chose to invite, the person who enters it chooses to accept.</summary>
    private static async Task<IResult> Accept(CodeRequest req, ClaimsPrincipal principal, AppDbContext db, IOptions<LetterOptions> options, TimeProvider clock, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var now = clock.GetUtcNow().UtcDateTime;
        var invite = await Find(db, req.Code, now, ct);
        if (invite is null) return NoSuchCode(ctx);
        var other = invite.InviterId;
        if (other == me) return Err(ctx, 422, ErrorCodes.InvalidState, "這是你自己的邀請碼，要交給朋友喔。");

        var (low, high) = Ordered(me, other);
        var existing = await db.Relationships.FirstOrDefaultAsync(r => r.UserLowId == low && r.UserHighId == high, ct);
        if (existing is { EndedAt: null }) return Err(ctx, 422, ErrorCodes.InvalidState, "你們已經是好友了。");
        var cap = options.Value.MaxRelationships;
        foreach (var user in new[] { me, other })
        {
            var mine = ActiveOf(db, user);
            if (await mine.CountAsync(ct) >= cap) return Err(ctx, 422, ErrorCodes.InvalidState, user == me ? $"你的好友已經滿 {cap} 位了。" : $"對方的好友已經滿 {cap} 位了。");
            if (invite.Kind == RelationKinds.Couple && await mine.AnyAsync(r => r.Kind == RelationKinds.Couple, ct))
                return Err(ctx, 422, ErrorCodes.InvalidState, user == me ? "你已經有一位情侶了。" : "對方已經有一位情侶了。");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var claimed = await db.PairingInvites.Where(i => i.InviteId == invite.InviteId && i.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UsedAt, now).SetProperty(i => i.UsedBy, me), ct);
        if (claimed == 0) return NoSuchCode(ctx);                      // someone used it a moment ago: a code works once
        if (existing is null) db.Relationships.Add(new Relationship { UserLowId = low, UserHighId = high, Kind = invite.Kind, CreatedAt = now });
        else { existing.EndedAt = null; existing.Kind = invite.Kind; existing.CreatedAt = now; }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Err(ctx, 422, ErrorCodes.InvalidState, "你們已經是好友了。");        // two accepts raced: the unique pair decides
        }
        await tx.CommitAsync(ct);
        var rel = existing ?? await db.Relationships.AsNoTracking().FirstAsync(r => r.UserLowId == low && r.UserHighId == high, ct);
        return Results.Ok(await ToDto(db, rel, me, ct));
    }

    internal static async Task<RelationshipDto> ToDto(AppDbContext db, Relationship r, Guid me, CancellationToken ct)
    {
        var other = r.Other(me);
        var name = await db.Users.AsNoTracking().Where(u => u.Id == other).Select(u => u.DisplayName).FirstOrDefaultAsync(ct) ?? "";
        var pet = await db.Pets.AsNoTracking().Where(p => p.OwnerId == other).Select(p => new { p.Name, p.BodyVariant }).FirstOrDefaultAsync(ct);
        return new RelationshipDto(r.RelationshipId, r.Kind, r.CreatedAt, new FriendDto(other, name, pet?.Name, pet?.BodyVariant));
    }

    private static async Task<IResult> ListRelationships(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var me = UserId(principal);
        var rels = await ActiveOf(db, me).AsNoTracking().OrderBy(r => r.CreatedAt).ToListAsync(ct);
        var list = new List<RelationshipDto>();
        foreach (var r in rels) list.Add(await ToDto(db, r, me, ct));
        return Results.Ok(new { relationships = list });
    }

    private static async Task<IResult> EndRelationship(Guid id, ClaimsPrincipal principal, AppDbContext db, TimeProvider clock, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var now = clock.GetUtcNow().UtcDateTime;
        var n = await db.Relationships.Where(r => r.RelationshipId == id && r.EndedAt == null && (r.UserLowId == me || r.UserHighId == me))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.EndedAt, now), ct);       // letters already delivered stay in each mailbox
        return n == 0 ? Err(ctx, 404, ErrorCodes.NotFound, "找不到這段關係。") : Results.NoContent();
    }
}
