using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PixelMate.Api.Data;

namespace PixelMate.Api.Life;

/// <summary>
/// Low-frequency background worker: every few minutes it makes up the finished slots of pets that are behind, so items arrive
/// even when no device is open. It writes only when something happened (cursor moves + events), never per second.
/// </summary>
public sealed class LifeWorker(IServiceScopeFactory scopes, IOptions<LifeOptions> options, TimeProvider clock, ILogger<LifeWorker> log) : BackgroundService
{
    private const int Batch = 100;

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var seconds = options.Value.WorkerIntervalSeconds;
        if (seconds <= 0) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        do
        {
            try { await RunOnce(stop); }
            catch (Exception e) when (e is not OperationCanceledException) { log.LogWarning(e, "Life worker pass failed; trying again next time."); }
        } while (await timer.WaitForNextTickAsync(stop));
    }

    public async Task<int> RunOnce(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var last = LifeScheduler.SlotOf(now) - 1;
        var total = 0;
        while (!ct.IsCancellationRequested)
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var life = scope.ServiceProvider.GetRequiredService<LifeService>();
            var behind = await (from p in db.Pets.AsNoTracking()
                                join l in db.PetLives.AsNoTracking() on p.PetId equals l.PetId into ls
                                from l in ls.DefaultIfEmpty()
                                where l == null || l.CursorSlot < last
                                orderby p.PetId
                                select new { p.PetId, p.OwnerId, p.CreatedAt }).Take(Batch).ToListAsync(ct);
            if (behind.Count == 0) break;
            foreach (var p in behind)
            {
                total += await life.CatchUpAsync(db, p.PetId, p.OwnerId, p.CreatedAt, now, ct);
                db.ChangeTracker.Clear();
            }
            if (behind.Count < Batch) break;
        }
        return total;
    }
}
