namespace PixelMate.Api.Life;

/// <summary><c>pet_life</c>: how far a pet's life has been made up. <see cref="CursorSlot"/> is a concurrency token, so only one writer advances a range.</summary>
public class PetLife
{
    public Guid PetId { get; set; }
    /// <summary>Last finished slot that has been processed.</summary>
    public long CursorSlot { get; set; }
}

/// <summary><c>pet_events</c>: things that happened to a pet while it lived. (pet_id, slot) is unique: one event per slot, however many times it is triggered.</summary>
public class PetEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid PetId { get; set; }
    public long Slot { get; set; }
    public string Kind { get; set; } = "find";
    public string ItemId { get; set; } = "";
    public DateTime OccurredAt { get; set; }
}

public sealed record PetEventDto(Guid EventId, string Kind, string ItemId, string Name, string AssetKey, string Rarity, DateTime OccurredAt);
