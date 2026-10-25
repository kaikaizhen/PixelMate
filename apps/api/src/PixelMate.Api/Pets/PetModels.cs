using FluentValidation;

namespace PixelMate.Api.Pets;

public sealed record CreatePetRequest(string Name, string BodyVariant);

public sealed record PetSnapshot(
    Guid PetId, string Name, string BodyVariant, int Level, int Xp, int Revision,
    IDictionary<string, int> Skills, int RemainingSkillPoints,
    IDictionary<string, string?> Equipment, PetWorld World, DateTime UpdatedAt, PixelMate.Api.Life.ActivityInfo Activity);

public sealed record PetWorld(string Mood, string Scene, string Weather, double? TemperatureC = null, string WeatherSource = "simulated", string TimeZone = "Asia/Taipei");

public sealed record AllocateSkillRequest(string SkillKey, int PointsToAdd, int ExpectedRevision);
public sealed record ResetSkillsRequest(int ExpectedRevision);
public sealed record SkillsResult(int Revision, IDictionary<string, int> Skills, int RemainingSkillPoints);

public sealed class CreatePetValidator : AbstractValidator<CreatePetRequest>
{
    public CreatePetValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("請幫小夥伴取個名字。")
            .Must(n => PetRules.NameLength(n) is >= 1 and <= 12).WithMessage("名字最多 12 個字。")
            .Must(PetRules.NameCharsAllowed).WithMessage("名字只能用文字、數字、空格和 - _ ・。");
        RuleFor(x => x.BodyVariant).Must(v => BodyVariants.All.Contains(v)).WithMessage("請選擇一個外觀。");
    }
}

public sealed class AllocateSkillValidator : AbstractValidator<AllocateSkillRequest>
{
    public AllocateSkillValidator()
    {
        RuleFor(x => x.SkillKey).Must(k => SkillKeys.All.Contains(k)).WithMessage("沒有這個技能。");
        RuleFor(x => x.PointsToAdd).InclusiveBetween(1, SkillKeys.MaxPointsPerSkill).WithMessage("一次加 1 到 5 點。");
        RuleFor(x => x.ExpectedRevision).GreaterThanOrEqualTo(1).WithMessage("缺少版本號。");
    }
}

public sealed class ResetSkillsValidator : AbstractValidator<ResetSkillsRequest>
{
    public ResetSkillsValidator() => RuleFor(x => x.ExpectedRevision).GreaterThanOrEqualTo(1).WithMessage("缺少版本號。");
}

public static class PetRules
{
    public static int NameLength(string? name) => name is null ? 0 : new System.Globalization.StringInfo(name.Trim()).LengthInTextElements;

    /// <summary>Letters (any script), digits, spaces and a few separators; no markup, control or private-use characters.</summary>
    public static bool NameCharsAllowed(string? name) =>
        name is not null && name.Trim().All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '·' or '・');

    /// <summary>Skill points a pet may spend: one per level above 1 (configurable later).</summary>
    public static int TotalSkillPoints(int level) => Math.Max(0, level - 1);
}
