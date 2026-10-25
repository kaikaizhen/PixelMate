using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PixelMate.Api.Realtime;

/// <summary>Wakes the dispatcher as soon as a unit of work that wrote outbox rows has really been committed.</summary>
public sealed class PokeInterceptor(OutboxSignal signal) : SaveChangesInterceptor, IDbTransactionInterceptor
{
    private static readonly ConditionalWeakTable<DbContext, object> Wrote = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var ctx = eventData.Context;
        if (ctx is not null && ctx.ChangeTracker.Entries<OutboxEvent>().Any(e => e.State == EntityState.Added)) Wrote.TryAdd(ctx, new object());
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context is { } ctx && ctx.Database.CurrentTransaction is null && Wrote.Remove(ctx)) signal.Poke();   // inside a transaction: wait for the commit
        return base.SavedChangesAsync(eventData, result, ct);
    }

    public Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken ct = default)
    {
        if (eventData.Context is { } ctx && Wrote.Remove(ctx)) signal.Poke();
        return Task.CompletedTask;
    }
}
