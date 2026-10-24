using FluentValidation;

namespace PixelMate.Api.Pets;

public sealed record CreatePetRequest(string Name, string BodyVariant);

public sealed record PetSnapshot(
    Guid PetId, string Name, string BodyVariant, int Level, int Xp, int Revision,
    IDictionary<string, int> Skills, int RemainingSkillPoints,
    IDictionary<string, string?> Equipment, PetWorld World, DateTime UpdatedAt);

public sealed record PetWorld(string Mood, string Scene, string Weather);

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

public static class PetRules
{
    public static int NameLength(string? name) => name is null ? 0 : new System.Globalization.StringInfo(name.Trim()).LengthInTextElements;

    /// <summary>Letters (any script), digits, spaces and a few separators; no markup, control or private-use characters.</summary>
    public static bool NameCharsAllowed(string? name) =>
        name is not null && name.Trim().All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '·' or '・');

    /// <summary>Skill points a pet may spend: one per level above 1 (configurable later).</summary>
    public static int TotalSkillPoints(int level) => Math.Max(0, level - 1);
}
