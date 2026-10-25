using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PixelMate.Api.Data;

namespace PixelMate.Api.Realtime;

/// <summary>Lets writers say "something new is committed" so the dispatcher acts at once instead of waiting for its timer.</summary>
public sealed class OutboxSignal
{
    private readonly SemaphoreSlim _gate = new(0, int.MaxValue);
    public void Poke() { if (_gate.CurrentCount < 1) _gate.Release(); }
    public Task WaitAsync(TimeSpan timeout, CancellationToken ct) => _gate.WaitAsync(timeout, ct);
}

/// <summary>
/// Turns committed outbox rows into <c>pet.event</c> messages for the owner's connections, and announces letters when the postman arrives.
/// If nobody is connected the row simply stays available to <c>GET /events?after=</c>; clients de-duplicate by eventId.
/// </summary>
public sealed class OutboxDispatcher(IServiceScopeFactory scopes, IHubContext<PetHub> hub, OutboxSignal signal, IOptions<RealtimeOptions> options, TimeProvider clock, ILogger<OutboxDispatcher> log) : BackgroundService
{
    private DateTime _lastPurge = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var seconds = options.Value.DispatchIntervalSeconds;
        if (seconds <= 0) return;
        while (!stop.IsCancellationRequested)
        {
            try { await DispatchOnce(stop); }
            catch (Exception e) when (e is not OperationCanceledException) { log.LogWarning(e, "Event dispatch failed; trying again shortly."); }
            try { await signal.WaitAsync(TimeSpan.FromSeconds(seconds), stop); }
            catch (OperationCanceledException) { break; }
        }
    }

    public async Task<int> DispatchOnce(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = clock.GetUtcNow().UtcDateTime;

        // 1. letters whose postman has arrived: tell the recipient once (the UPDATE decides who announces it when several servers run)
        var arrived = await db.Letters.AsNoTracking().Where(l => l.DeliverAt <= now && l.NotifiedAt == null).OrderBy(l => l.DeliverAt).Select(l => new { l.LetterId, l.RecipientId }).Take(200).ToListAsync(ct);
        var outbox = new EventOutbox(db, clock);
        foreach (var l in arrived)
        {
            var won = await db.Letters.Where(x => x.LetterId == l.LetterId && x.NotifiedAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.NotifiedAt, now), ct);
            if (won == 1) outbox.Add(l.RecipientId, EventTypes.MailReceived, new { letterId = l.LetterId });
        }
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);

        // 2. push what is committed and not yet pushed
        var pending = await db.OutboxEvents.Where(e => e.DispatchedAt == null).OrderBy(e => e.Seq).Take(200).ToListAsync(ct);
        foreach (var e in pending)
        {
            var message = new PetEventMessage(e.Seq, e.EventId, e.Type, e.PetId, e.Revision, e.OccurredAt, JsonDocument.Parse(e.PayloadJson).RootElement.Clone());
            await hub.Clients.Group(PetHub.Group(e.UserId)).SendAsync(PetHub.Event, message, ct);
            e.DispatchedAt = now;
        }
        if (pending.Count > 0) await db.SaveChangesAsync(ct);

        // 3. old, delivered rows go away (clients that were offline longer re-read the API instead)
        if (now - _lastPurge > TimeSpan.FromHours(1))
        {
            _lastPurge = now;
            var cutoff = now.AddDays(-options.Value.KeepDays);
            await db.OutboxEvents.Where(e => e.DispatchedAt != null && e.DispatchedAt < cutoff).ExecuteDeleteAsync(ct);
        }
        return pending.Count;
    }
}
