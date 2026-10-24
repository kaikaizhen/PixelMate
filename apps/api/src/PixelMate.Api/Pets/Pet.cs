namespace PixelMate.Api.Pets;

/// <summary><c>pets</c> table: one main pet per owner (unique owner_id). Times are UTC.</summary>
public class Pet
{
    public Guid PetId { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string BodyVariant { get; set; } = "";
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    /// <summary>Bumped on every state write so PWA / ESP32 can tell when their snapshot is stale.</summary>
    public int Revision { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<PetSkill> Skills { get; set; } = [];
    public List<PixelMate.Api.Items.PetEquipment> Equipment { get; set; } = [];
}

/// <summary><c>pet_skills</c>: points per skill, 0..5 (database check constraint + service rule).</summary>
public class PetSkill
{
    public Guid PetId { get; set; }
    public string SkillKey { get; set; } = "";
    public int Points { get; set; }
}

public static class SkillKeys
{
    public static readonly string[] All = ["empathy", "creativity", "exploration", "wisdom"];
    public const int MaxPointsPerSkill = 5;
}

public static class BodyVariants
{
    /// <summary>The star pet in the colours that have art (assets/manifest.json: pet.star_01.body.*).</summary>
    public static readonly string[] All = ["star_01_cream", "star_01_mint"];
}
