using System.Text.Json;
using PixelMate.Api.Data;

namespace PixelMate.Api.Realtime;

/// <summary>Adds an event to the current unit of work: it is saved (or lost) together with the change that caused it.</summary>
public sealed class EventOutbox(AppDbContext db, TimeProvider clock)
{
    public void Add(Guid userId, string type, object? payload = null, Guid? petId = null, int? revision = null) =>
        db.OutboxEvents.Add(new OutboxEvent
        {
            UserId = userId, Type = type, PetId = petId, Revision = revision, OccurredAt = clock.GetUtcNow().UtcDateTime,
            PayloadJson = payload is null ? "{}" : JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        });
}
