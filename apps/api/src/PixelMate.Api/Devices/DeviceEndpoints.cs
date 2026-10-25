using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PixelMate.Api.Data;
using PixelMate.Api.Infrastructure;
using PixelMate.Api.Life;
using PixelMate.Api.Pets;
using PixelMate.Api.Realtime;
using PixelMate.Api.Social;

namespace PixelMate.Api.Devices;

/// <summary>
/// Companion devices (ESP32). Pairing: the device asks for a code and shows it, the owner types the code into the app, the device then collects a credential
/// of its own, exactly once. After that it reads its pet's snapshot, events and mail with that credential; the owner can revoke it any time.
/// </summary>
public static class DeviceEndpoints
{
    public static RouteGroupBuilder MapDevices(this RouteGroupBuilder api)
    {
        // what a device itself calls
        var dev = api.MapGroup("/device");
        dev.MapPost("/pairing/start", Start).Validate<StartPairingRequest>().RequireRateLimiting("pairing");
        dev.MapPost("/pairing/poll", Poll).Validate<PollPairingRequest>().RequireRateLimiting("devicepoll");

        var authed = dev.MapGroup("").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = DeviceCredentials.Scheme });
        authed.MapGet("/snapshot", Snapshot);
        authed.MapGet("/events", EventEndpoints.Catchup);
        authed.MapGet("/mail", MailEndpoints.List);
        authed.MapGet("/mail/{id:guid}", MailEndpoints.Get);
        authed.MapPost("/mail/{id:guid}/read", MailEndpoints.Read);
        authed.MapPost("/credential/rotate", Rotate);
        authed.MapDelete("/me", Unpair);

        // what the owner does in the app (cookie session)
        var mine = api.MapGroup("/devices").RequireAuthorization();
        mine.MapPost("/pair", Approve).Validate<ApprovePairingRequest>().RequireRateLimiting("pairing");
        mine.MapGet("/", List);
        mine.MapPut("/{id:guid}", Rename).Validate<RenameDeviceRequest>();
        mine.MapDelete("/{id:guid}", Revoke);
        return api;
    }

    private static Guid UserId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static Guid DeviceId(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue("device_id")!);
    private static IResult Err(HttpContext ctx, int status, string code, string message) => Results.Json(new ApiError(code, message, ctx.TraceIdentifier), statusCode: status);
    private static DeviceDto ToDto(Device d) => new(d.DeviceId, d.PetId, d.Model, d.Name, d.State, d.CreatedAt, d.ActivatedAt, d.LastSeenAt);
    private static string NewPollToken() => DeviceCredentials.Base64Url(RandomNumberGenerator.GetBytes(24));

    // ---- the device asks to be paired ---------------------------------------------------------------------------------
    private static async Task<IResult> Start(StartPairingRequest req, AppDbContext db, IOptions<DeviceOptions> options, TimeProvider clock, ILogger<Program> log, CancellationToken ct)
    {
        var o = options.Value;
        var now = clock.GetUtcNow().UtcDateTime;
        await db.DevicePairings.Where(p => p.ExpiresAt < now.AddHours(-1)).ExecuteDeleteAsync(ct);          // tidy up old requests
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = PairingEndpoints.NewCode();
            var pollToken = NewPollToken();
            var p = new DevicePairing
            {
                CodeHash = PairingEndpoints.Hash(PairingEndpoints.Normalize(code)), PollTokenHash = DeviceCredentials.Hash(pollToken),
                Model = req.Model.Trim(), CreatedAt = now, ExpiresAt = now.AddMinutes(o.PairingMinutes),
            };
            db.DevicePairings.Add(p);
            try
            {
                await db.SaveChangesAsync(ct);
                log.LogInformation("Device pairing {PairingId} started ({Model})", p.PairingId, p.Model);       // never the code or the token
                return Results.Ok(new PairingStarted(p.PairingId, code, pollToken, p.ExpiresAt, o.PollIntervalSeconds));
            }
            catch (DbUpdateException) { db.ChangeTracker.Clear(); }                                        // that code hash exists (very unlikely): draw another
        }
        return Results.Json(new ApiError(ErrorCodes.ServiceUnavailable, "暫時無法產生配對碼。", "-"), statusCode: 503);
    }

    // ---- the device waits for the owner, then collects its credential once ------------------------------------------------
    private static async Task<IResult> Poll(PollPairingRequest req, AppDbContext db, EventOutbox outbox, TimeProvider clock, HttpContext ctx, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var p = await db.DevicePairings.AsNoTracking().FirstOrDefaultAsync(x => x.PairingId == req.PairingId, ct);
        if (p is null || !DeviceCredentials.Matches(p.PollTokenHash, req.PollToken)) return Err(ctx, 404, ErrorCodes.NotFound, "找不到這個配對。");   // same answer for a wrong id and a wrong token
        if (p.ApprovedAt is null) return Results.Ok(new PairingStatus(p.ExpiresAt <= now ? "expired" : "pending"));
        if (p.DeliveredAt is not null) return Results.Ok(new PairingStatus("delivered"));

        var device = await db.Devices.FirstOrDefaultAsync(d => d.DeviceId == p.DeviceId, ct);
        if (device is null || device.RevokedAt is not null) return Results.Ok(new PairingStatus("revoked"));

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var won = await db.DevicePairings.Where(x => x.PairingId == p.PairingId && x.DeliveredAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.DeliveredAt, now), ct);
        if (won == 0) return Results.Ok(new PairingStatus("delivered"));                                    // another poll collected it a moment ago
        var credential = DeviceCredentials.New(device.DeviceId);
        device.CredentialHash = DeviceCredentials.Hash(credential);
        device.ActivatedAt = now;
        outbox.Add(device.OwnerId, EventTypes.DevicesUpdated, new { deviceId = device.DeviceId, state = "active" });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Results.Ok(new PairingStatus("approved", device.DeviceId, credential, device.PetId));          // the only time the plain credential exists
    }

    // ---- what a paired device reads ---------------------------------------------------------------------------------------
    private static async Task<IResult> Snapshot(ClaimsPrincipal principal, AppDbContext db, LifeService life, TimeProvider clock, IOptions<LifeOptions> lifeOptions, HttpContext ctx, CancellationToken ct)
    {
        var owner = UserId(principal);
        var snap = await PetEndpoints.SnapshotOf(owner, db, life, clock, lifeOptions.Value, ctx, ct, Guid.Parse(principal.FindFirstValue("pet_id")!));   // the pet this device is bound to
        if (snap is null) return Err(ctx, 404, ErrorCodes.NotFound, "找不到角色。");
        var now = clock.GetUtcNow().UtcDateTime;
        var unread = await db.Letters.CountAsync(l => l.RecipientId == owner && !l.RecipientDeleted && l.ReadAt == null && l.DeliverAt <= now, ct);
        var latest = await db.OutboxEvents.Where(e => e.UserId == owner).MaxAsync(e => (long?)e.Seq, ct) ?? 0;
        return Results.Ok(new { pet = snap, unreadMail = unread, latestEventSeq = latest, serverTime = now });
    }

    private static async Task<IResult> Rotate(ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var id = DeviceId(principal);
        var credential = DeviceCredentials.New(id);
        var n = await db.Devices.Where(d => d.DeviceId == id && d.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(d => d.CredentialHash, DeviceCredentials.Hash(credential)), ct);
        return n == 0 ? Err(ctx, 401, ErrorCodes.AuthRequired, "裝置憑證已失效。") : Results.Ok(new { credential });   // the old one stops working at once
    }

    private static async Task<IResult> Unpair(ClaimsPrincipal principal, AppDbContext db, EventOutbox outbox, TimeProvider clock, CancellationToken ct)
    {
        await RevokeCore(DeviceId(principal), UserId(principal), db, outbox, clock, ct);
        return Results.NoContent();
    }

    private static async Task<bool> RevokeCore(Guid deviceId, Guid owner, AppDbContext db, EventOutbox outbox, TimeProvider clock, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var n = await db.Devices.Where(d => d.DeviceId == deviceId && d.OwnerId == owner && d.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.RevokedAt, now).SetProperty(d => d.CredentialHash, (string?)null), ct);
        if (n == 0) return false;
        outbox.Add(owner, EventTypes.DevicesUpdated, new { deviceId, state = "revoked" });
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ---- the owner, in the app --------------------------------------------------------------------------------------------
    private static async Task<IResult> Approve(ApprovePairingRequest req, ClaimsPrincipal principal, AppDbContext db, EventOutbox outbox, IOptions<DeviceOptions> options, TimeProvider clock, HttpContext ctx, ILogger<Program> log, CancellationToken ct)
    {
        var me = UserId(principal);
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = PairingEndpoints.Hash(PairingEndpoints.Normalize(req.Code));
        var p = await db.DevicePairings.AsNoTracking().FirstOrDefaultAsync(x => x.CodeHash == hash && x.ApprovedAt == null && x.ExpiresAt > now, ct);
        if (p is null) return Err(ctx, 404, ErrorCodes.NotFound, "找不到這組配對碼，或已經過期了。");
        var petId = await db.Pets.AsNoTracking().Where(x => x.OwnerId == me).Select(x => (Guid?)x.PetId).FirstOrDefaultAsync(ct);
        if (petId is null) return Err(ctx, 422, ErrorCodes.InvalidState, "請先領養小夥伴，再配對裝置。");
        var cap = options.Value.MaxDevicesPerPet;
        if (await db.Devices.CountAsync(d => d.PetId == petId && d.RevokedAt == null, ct) >= cap) return Err(ctx, 422, ErrorCodes.InvalidState, $"一隻小夥伴最多配對 {cap} 台裝置，請先移除不用的。");

        var device = new Device { PetId = petId.Value, OwnerId = me, Model = p.Model, Name = string.IsNullOrWhiteSpace(req.Name) ? p.Model : req.Name.Trim(), CreatedAt = now };
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var won = await db.DevicePairings.Where(x => x.PairingId == p.PairingId && x.ApprovedAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.ApprovedAt, now).SetProperty(x => x.DeviceId, device.DeviceId), ct);
        if (won == 0) return Err(ctx, 404, ErrorCodes.NotFound, "找不到這組配對碼，或已經過期了。");           // a code works once
        db.Devices.Add(device);
        outbox.Add(me, EventTypes.DevicesUpdated, new { deviceId = device.DeviceId, state = "waiting" });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        log.LogInformation("Device {DeviceId} approved for pet {PetId}", device.DeviceId, device.PetId);
        return Results.Ok(ToDto(device));
    }

    private static async Task<IResult> List(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var me = UserId(principal);
        var rows = await db.Devices.AsNoTracking().Where(d => d.OwnerId == me && d.RevokedAt == null).OrderBy(d => d.CreatedAt).ToListAsync(ct);
        return Results.Ok(new { devices = rows.Select(ToDto).ToList() });
    }

    private static async Task<IResult> Rename(Guid id, RenameDeviceRequest req, ClaimsPrincipal principal, AppDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var me = UserId(principal);
        var n = await db.Devices.Where(d => d.DeviceId == id && d.OwnerId == me && d.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(d => d.Name, req.Name.Trim()), ct);
        return n == 0 ? Err(ctx, 404, ErrorCodes.NotFound, "找不到這台裝置。") : Results.NoContent();
    }

    private static async Task<IResult> Revoke(Guid id, ClaimsPrincipal principal, AppDbContext db, EventOutbox outbox, TimeProvider clock, HttpContext ctx, CancellationToken ct) =>
        await RevokeCore(id, UserId(principal), db, outbox, clock, ct) ? Results.NoContent() : Err(ctx, 404, ErrorCodes.NotFound, "找不到這台裝置。");
}
