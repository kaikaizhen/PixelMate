using FluentValidation;

namespace PixelMate.Api.Rooms;

/// <summary><c>room_states</c>: a player's room as a whole (floor + version). Created, with the starter table, the first time the room is read.</summary>
public class RoomState
{
    public Guid OwnerId { get; set; }
    public string? FloorItemId { get; set; }
    public int Revision { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary><c>room_placements</c>: one owned item standing in the room. (X, Y) is its top-left corner on the 240x240 logical canvas.</summary>
public class RoomPlacement
{
    public Guid PlacementId { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string ItemId { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
}

public static class RoomRules
{
    public const int Canvas = 240;
    public const int MaxPlacements = 40;
    public const string StarterItem = "world.furniture.table_garden";
    public const int StarterX = 150, StarterY = 100;
    public static readonly string[] PlaceableCategories = ["furniture", "decor", "collectible"];
    public static bool IsFloor(string itemId) => itemId.StartsWith("world.decor.floor_tile_", StringComparison.Ordinal);
}

public sealed record PlacementInput(string ItemId, int X, int Y);
public sealed record SaveRoomRequest(int ExpectedRevision, string? FloorItemId, List<PlacementInput> Placements);
public sealed record PlacementDto(Guid PlacementId, string ItemId, int X, int Y);
public sealed record RoomDto(int Revision, string? FloorItemId, IReadOnlyList<PlacementDto> Placements);

public sealed class SaveRoomValidator : AbstractValidator<SaveRoomRequest>
{
    public SaveRoomValidator()
    {
        RuleFor(x => x.ExpectedRevision).GreaterThanOrEqualTo(1).WithMessage("缺少版本號。");
        RuleFor(x => x.Placements).NotNull().Must(p => p is null || p.Count <= RoomRules.MaxPlacements).WithMessage($"小屋最多放 {RoomRules.MaxPlacements} 件。");
        RuleForEach(x => x.Placements).ChildRules(p =>
        {
            p.RuleFor(i => i.ItemId).NotEmpty().MaximumLength(80);
            p.RuleFor(i => i.X).InclusiveBetween(0, RoomRules.Canvas).WithMessage("位置超出小屋了。");
            p.RuleFor(i => i.Y).InclusiveBetween(0, RoomRules.Canvas).WithMessage("位置超出小屋了。");
        });
        RuleFor(x => x.FloorItemId).MaximumLength(80);
    }
}
