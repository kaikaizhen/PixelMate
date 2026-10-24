using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Items;
using static PixelMate.Api.Social.PairingEndpoints;

namespace PixelMate.Api.Social;

public static class MailEndpoints
{
    public static RouteGroupBuilder MapMail(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/mail").RequireAuthorization();
        g.MapPost("/", Send).Validate<SendLetterRequest>().RequireRateLimiting("pairing");
        g.MapGet("/", List);
        g.MapGet("/unread", Unread);
        g.MapGet("/{id:guid}", Get);
        g.MapPost("/{id:guid}/read", Read);
        g.MapDelete("/{id:guid}", Delete);
        return api;
    }

    private static LetterDto ToDto(Letter l) => new(l.LetterId, l.SenderId, l.RecipientId, l.Body.Trim(), l.SentAt, l.DeliverAt, l.ReadAt, l.PaperItemId, l.GiftItemId);

    /// <summary>The postman's walk: a fixed but per-letter delay between min and max seconds.</summary>
    private static int DelaySeconds(Guid letterId, LetterOptions o)
    {
        var span = Math.Max(0, o.MaxDelaySeconds - o.MinDelaySeconds);
        return o.MinDelaySeconds + (span == 0 ? 0 : (int)(BitConverter.ToUInt32(SHA256.HashData(letterId.ToByteArray()), 0) % (uint)(span + 1)));
    }

    /// <summary>Sends a letter. The gift leaves the sender's bag now and reaches the recipient when they open the letter. Idempotency-Key makes a retry return the same letter.</summary>
    private static async Task<IResult> Send(SendLetterRequest req, System.Security.Claims.ClaimsPrincipal principal, AppDbContext db, IOptions<LetterOptions> options, TimeProvider clock, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var o = options.Value;
        var key = ctx.Request.Headers["Idempotency-Key"].ToString().Trim();
        if (key.Length is < 8 or > 64) return Err(ctx, 422, ErrorCodes.ValidationFailed, "缺少 Idempotency-Key（8 到 64 個字元）。");

        var again = await db.Letters.AsNoTracking().FirstOrDefaultAsync(l => l.SenderId == me && l.IdempotencyKey == key, ct);
        if (again is not null) return Results.Json(ToDto(again), statusCode: 201);        // same key: the original result, nothing new

        if (req.RecipientUserId == me) return Err(ctx, 422, ErrorCodes.InvalidState, "不能寄信給自己。");
        var (low, high) = Ordered(me, req.RecipientUserId);
        if (!await db.Relationships.AnyAsync(r => r.UserLowId == low && r.UserHighId == high && r.EndedAt == null, ct))
            return Err(ctx, 403, ErrorCodes.NotConnected, "你們還不是好友，不能寄信。");
        var now = clock.GetUtcNow().UtcDateTime;
        if (await db.Letters.CountAsync(l => l.SenderId == me && l.SentAt > now.AddHours(-24), ct) >= o.MaxPerDay)
            return Err(ctx, 429, ErrorCodes.RateLimited, "今天寄的信太多了，明天再寄吧。");

        if (req.PaperItemId is not null)
        {
            var paper = await (from i in db.InventoryEntries join d in db.ItemDefinitions on i.ItemId equals d.ItemId
                               where i.OwnerId == me && i.ItemId == req.PaperItemId && i.Quantity > 0 && d.Category == "stationery" select i.InventoryId).AnyAsync(ct);
            if (!paper) return Err(ctx, 422, ErrorCodes.InvalidState, "背包裡沒有這張信紙。");
        }

        var letter = new Letter { SenderId = me, RecipientId = req.RecipientUserId, Body = req.Body.Trim(), PaperItemId = req.PaperItemId, GiftItemId = req.GiftItemId, SentAt = now, IdempotencyKey = key };
        letter.DeliverAt = now.AddSeconds(DelaySeconds(letter.LetterId, o));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (req.GiftItemId is not null)
        {
            var row = await db.InventoryEntries.FirstOrDefaultAsync(i => i.OwnerId == me && i.ItemId == req.GiftItemId && i.Quantity > 0, ct);
            if (row is null) return Err(ctx, 422, ErrorCodes.InvalidState, "背包裡沒有這個禮物。");
            var worn = await db.PetEquipment.CountAsync(e => e.InventoryId == row.InventoryId, ct);
            if (worn > 0 && row.Quantity <= 1) return Err(ctx, 422, ErrorCodes.InvalidState, "這件正穿在小夥伴身上，請先脫下來再送。");
            row.Quantity -= 1;                                          // out of the sender's bag in the same transaction as the letter
        }
        db.Letters.Add(letter);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var existing = await db.Letters.AsNoTracking().FirstOrDefaultAsync(l => l.SenderId == me && l.IdempotencyKey == key, ct);
            if (existing is not null) return Results.Json(ToDto(existing), statusCode: 201);       // a double tap raced: keep the first one
            throw;
        }
        await tx.CommitAsync(ct);
        return Results.Created($"/api/v1/mail/{letter.LetterId}", ToDto(letter));
    }

    private static IQueryable<Letter> Inbox(AppDbContext db, Guid me, DateTime now) =>
        db.Letters.Where(l => l.RecipientId == me && !l.RecipientDeleted && l.DeliverAt <= now);

    private static async Task<IResult> Unread(System.Security.Claims.ClaimsPrincipal principal, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var me = UserId(principal);
        var now = clock.GetUtcNow().UtcDateTime;
        var q = Inbox(db, me, now).Where(l => l.ReadAt == null);
        return Results.Ok(new { unread = await q.CountAsync(ct), newestDeliveredAt = await q.MaxAsync(l => (DateTime?)l.DeliverAt, ct) });
    }

    /// <summary>box=inbox (default; only letters whose postman has arrived) or box=sent. Newest first; cursor = DeliverAt ticks of the last row.</summary>
    private static async Task<IResult> List(System.Security.Claims.ClaimsPrincipal principal, AppDbContext db, TimeProvider clock, string? box, bool? unreadOnly, int? limit, string? cursor, CancellationToken ct)
    {
        var me = UserId(principal);
        var now = clock.GetUtcNow().UtcDateTime;
        var take = Math.Clamp(limit ?? 20, 1, 50);
        var sent = box == "sent";
        var q = sent ? db.Letters.Where(l => l.SenderId == me && !l.SenderDeleted) : Inbox(db, me, now);
        if (!sent && unreadOnly == true) q = q.Where(l => l.ReadAt == null);
        if (long.TryParse(cursor, out var ticks)) { var before = new DateTime(ticks, DateTimeKind.Utc); q = q.Where(l => l.DeliverAt < before); }
        var rows = await q.AsNoTracking().OrderByDescending(l => l.DeliverAt).ThenBy(l => l.LetterId).Take(take + 1).ToListAsync(ct);
        var page = rows.Take(take).ToList();
        var ids = page.Select(l => sent ? l.RecipientId : l.SenderId).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
        var list = page.Select(l =>
        {
            var other = sent ? l.RecipientId : l.SenderId;
            return new LetterSummary(l.LetterId, other, names.GetValueOrDefault(other, ""), LetterRules.Preview(l.Body), l.SentAt, l.DeliverAt, l.DeliverAt <= now, l.ReadAt, l.GiftItemId is not null, l.PaperItemId);
        }).ToList();
        var unread = await Inbox(db, me, now).CountAsync(l => l.ReadAt == null, ct);
        return Results.Ok(new MailList(list, unread, rows.Count > take ? page[^1].DeliverAt.Ticks.ToString() : null));
    }

    private static async Task<GiftDto?> GiftOf(AppDbContext db, string? itemId, CancellationToken ct) =>
        itemId is null ? null : await db.ItemDefinitions.AsNoTracking().Where(d => d.ItemId == itemId).Select(d => new GiftDto(d.ItemId, d.Name, d.AssetKey, d.Rarity)).FirstOrDefaultAsync(ct);

    private static async Task<IResult> Get(Guid id, System.Security.Claims.ClaimsPrincipal principal, AppDbContext db, TimeProvider clock, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var now = clock.GetUtcNow().UtcDateTime;
        var l = await db.Letters.AsNoTracking().FirstOrDefaultAsync(x => x.LetterId == id && ((x.SenderId == me && !x.SenderDeleted) || (x.RecipientId == me && !x.RecipientDeleted && x.DeliverAt <= now)), ct);
        if (l is null) return Err(ctx, 404, ErrorCodes.NotFound, "找不到這封信。");      // also for other people's letters: no hint that they exist
        var sender = await db.Users.AsNoTracking().Where(u => u.Id == l.SenderId).Select(u => u.DisplayName).FirstAsync(ct);
        var gift = await GiftOf(db, l.GiftItemId, ct);
        return Results.Ok(new LetterFull(l.LetterId, l.SenderId, sender, l.RecipientId, l.Body.Trim(), l.SentAt, l.DeliverAt, l.ReadAt, l.PaperItemId, gift, l.SenderId == me));
    }

    /// <summary>Opening a letter marks it read and hands over its gift, exactly once however often it is called.</summary>
    private static async Task<IResult> Read(Guid id, System.Security.Claims.ClaimsPrincipal principal, AppDbContext db, TimeProvider clock, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var now = clock.GetUtcNow().UtcDateTime;
        var l = await db.Letters.AsNoTracking().FirstOrDefaultAsync(x => x.LetterId == id && x.RecipientId == me && !x.RecipientDeleted && x.DeliverAt <= now, ct);
        if (l is null) return Err(ctx, 404, ErrorCodes.NotFound, "找不到這封信。");
        var gift = await GiftOf(db, l.GiftItemId, ct);
        if (l.ReadAt is not null) return Results.Ok(new ReadResult(l.ReadAt.Value, gift, false));

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var won = await db.Letters.Where(x => x.LetterId == id && x.ReadAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, now), ct);
        if (won == 0)
        {
            var read = await db.Letters.AsNoTracking().Where(x => x.LetterId == id).Select(x => x.ReadAt).FirstAsync(ct);   // another tab opened it first
            return Results.Ok(new ReadResult(read ?? now, gift, false));
        }
        if (l.GiftItemId is not null)
        {
            var row = await db.InventoryEntries.FirstOrDefaultAsync(i => i.OwnerId == me && i.ItemId == l.GiftItemId, ct);
            if (row is null) db.InventoryEntries.Add(new InventoryEntry { OwnerId = me, ItemId = l.GiftItemId, Quantity = 1 });
            else row.Quantity += 1;
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return Results.Ok(new ReadResult(now, gift, gift is not null));
    }

    /// <summary>The recipient deletes their copy once it was opened (so a gift is never lost); the sender deletes their sent copy any time.</summary>
    private static async Task<IResult> Delete(Guid id, System.Security.Claims.ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var l = await db.Letters.FirstOrDefaultAsync(x => x.LetterId == id && (x.SenderId == me || x.RecipientId == me), ct);
        if (l is null) return Err(ctx, 404, ErrorCodes.NotFound, "找不到這封信。");
        if (l.RecipientId == me && !l.RecipientDeleted)
        {
            if (l.ReadAt is null) return Err(ctx, 422, ErrorCodes.InvalidState, "這封信還沒打開，先看過再刪除吧。");
            l.RecipientDeleted = true;
        }
        else if (l.SenderId == me) l.SenderDeleted = true;
        if (l.SenderDeleted && l.RecipientDeleted) db.Letters.Remove(l);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
