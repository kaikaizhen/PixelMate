namespace PixelMate.Api.Realtime;

/// <summary><c>outbox_events</c>: something that happened to a player, written in the same transaction as the change itself and pushed afterwards.</summary>
public class OutboxEvent
{
    public long Seq { get; set; }                       // the order events are delivered and caught up in
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Type { get; set; } = "";
    public Guid? PetId { get; set; }
    public int? Revision { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime OccurredAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
}

/// <summary>What a client receives as <c>pet.event</c> (docs/API契約.md §7). It is only a hint to re-read the API.</summary>
public sealed record PetEventMessage(long Seq, Guid EventId, string Type, Guid? PetId, int? Revision, DateTime OccurredAt, System.Text.Json.JsonElement Payload);

public sealed class RealtimeOptions
{
    /// <summary>How often the dispatcher looks for undelivered events when nobody poked it; 0 turns the background service off.</summary>
    public int DispatchIntervalSeconds { get; set; } = 5;
    public int KeepDays { get; set; } = 7;
}

public static class EventTypes
{
    public const string PetUpdated = "pet.updated";          // skills, equipment, level: re-read /pets/me
    public const string PetFind = "pet.find";                // the pet found something on a walk
    public const string InventoryUpdated = "inventory.updated";
    public const string RoomUpdated = "room.updated";
    public const string MailReceived = "mail.received";
    public const string FriendsUpdated = "friends.updated";
    public const string DevicesUpdated = "devices.updated";    // a device was paired, activated or revoked
}
