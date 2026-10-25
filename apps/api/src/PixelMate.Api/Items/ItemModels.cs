using FluentValidation;

namespace PixelMate.Api.Items;

/// <summary><c>item_definitions</c>: the catalogue of everything a player can own. Seeded from the asset manifest; <c>ItemId</c> = assetKey.</summary>
public class ItemDefinition
{
    public string ItemId { get; set; } = "";
    public string Category { get; set; } = "";     // hat accessory shoes handheld costume furniture decor collectible stationery
    public string Rarity { get; set; } = "common"; // common | rare | precious (only affects drop odds and the frame colour)
    public string? Slot { get; set; }              // wearables only: head front back feet hand costume
    public string AssetKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string Batch { get; set; } = "";
    public string DesignId { get; set; } = "";
    public bool Tradable { get; set; } = true;
}

/// <summary><c>inventory_entries</c>: what a player owns; one stacked row per (owner, item).</summary>
public class InventoryEntry
{
    public Guid InventoryId { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary><c>pet_equipment</c>: what the pet wears; (pet_id, slot) is the key, so a slot holds at most one item.</summary>
public class PetEquipment
{
    public Guid PetId { get; set; }
    public string Slot { get; set; } = "";
    public Guid InventoryId { get; set; }
    /// <summary>Copy of the item id (the inventory row can be re-stacked), so snapshots need no join.</summary>
    public string ItemId { get; set; } = "";
}

public static class Slots
{
    public static readonly string[] Wearable = ["head", "front", "back", "feet", "hand", "costume"];
}

public sealed record InventoryItemDto(Guid InventoryId, string ItemId, string Name, string Category, string Rarity, string? Slot,
    string AssetKey, int Quantity, bool Equipped);

public sealed record InventoryResponse(IReadOnlyList<InventoryItemDto> Items);

public sealed record EquipRequest(string Slot, string? ItemId, int ExpectedRevision);
public sealed record EquipResult(int Revision, IDictionary<string, string?> Equipment);
public sealed record GrantRequest(string ItemId, int Quantity);

public sealed class EquipValidator : AbstractValidator<EquipRequest>
{
    public EquipValidator()
    {
        RuleFor(x => x.Slot).Must(s => Slots.Wearable.Contains(s)).WithMessage("沒有這個穿戴位置。");
        RuleFor(x => x.ExpectedRevision).GreaterThanOrEqualTo(1).WithMessage("缺少版本號。");
        RuleFor(x => x.ItemId).MaximumLength(80);
    }
}

public sealed class GrantValidator : AbstractValidator<GrantRequest>
{
    public GrantValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 99);
    }
}
