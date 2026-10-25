using FluentValidation;

namespace PixelMate.Api.Social;

public static class RelationKinds
{
    public const string Friend = "friend", Couple = "couple";
    public static readonly string[] All = [Friend, Couple];
}

/// <summary><c>pairing_invites</c>: a one-time code one player gives to another. Only a hash of the code is stored.</summary>
public class PairingInvite
{
    public Guid InviteId { get; set; } = Guid.NewGuid();
    public Guid InviterId { get; set; }
    public string CodeHash { get; set; } = "";
    public string Kind { get; set; } = RelationKinds.Friend;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid? UsedBy { get; set; }
}

/// <summary><c>relationships</c>: two players who confirmed each other. (low, high) is the ordered user pair, so a pair exists once; ending keeps the row.</summary>
public class Relationship
{
    public Guid RelationshipId { get; set; } = Guid.NewGuid();
    public Guid UserLowId { get; set; }
    public Guid UserHighId { get; set; }
    public string Kind { get; set; } = RelationKinds.Friend;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool Includes(Guid user) => user == UserLowId || user == UserHighId;
    public Guid Other(Guid user) => user == UserLowId ? UserHighId : UserLowId;
}

/// <summary><c>letters</c>: a letter and its (optional) gift. It reaches the recipient at <see cref="DeliverAt"/> (the postman's walk).</summary>
public class Letter
{
    public Guid LetterId { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public string Body { get; set; } = "";
    public string? PaperItemId { get; set; }
    public string? GiftItemId { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime DeliverAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool SenderDeleted { get; set; }
    public bool RecipientDeleted { get; set; }
    public string IdempotencyKey { get; set; } = "";
    /// <summary>Set when the recipient was told (mail.received) after the postman arrived.</summary>
    public DateTime? NotifiedAt { get; set; }
}

public sealed class LetterOptions
{
    public int MinDelaySeconds { get; set; } = 300;
    public int MaxDelaySeconds { get; set; } = 900;
    public int MaxPerDay { get; set; } = 30;
    public int MaxBodyLength { get; set; } = 500;
    public int MaxRelationships { get; set; } = 10;
    public int InviteHours { get; set; } = 24;
    public int MaxActiveInvites { get; set; } = 5;
}

// ---- requests / responses ---------------------------------------------------------------------------------------
public sealed record CreateInviteRequest(string Kind);
public sealed record CodeRequest(string Code);
public sealed record InviteCreated(Guid InviteId, string Code, string Kind, DateTime ExpiresAt);
public sealed record InviteDto(Guid InviteId, string Kind, DateTime CreatedAt, DateTime ExpiresAt);
public sealed record InvitePreview(string InviterName, string? InviterPetName, string Kind, DateTime ExpiresAt);
public sealed record FriendDto(Guid UserId, string DisplayName, string? PetName, string? BodyVariant);
public sealed record RelationshipDto(Guid RelationshipId, string Kind, DateTime Since, FriendDto Other);

public sealed record SendLetterRequest(Guid RecipientUserId, string Body, string? PaperItemId, string? GiftItemId);
public sealed record LetterDto(Guid LetterId, Guid SenderUserId, Guid RecipientUserId, string Body, DateTime SentAt, DateTime DeliverAt, DateTime? ReadAt, string? PaperItemId, string? GiftItemId);
public sealed record LetterSummary(Guid LetterId, Guid OtherUserId, string OtherDisplayName, string Preview, DateTime SentAt, DateTime DeliverAt, bool Delivered, DateTime? ReadAt, bool HasGift, string? PaperItemId);
public sealed record MailList(IReadOnlyList<LetterSummary> Letters, int Unread, string? NextCursor);
public sealed record GiftDto(string ItemId, string Name, string AssetKey, string Rarity);
public sealed record LetterFull(Guid LetterId, Guid SenderUserId, string SenderDisplayName, Guid RecipientUserId, string Body, DateTime SentAt, DateTime DeliverAt, DateTime? ReadAt, string? PaperItemId, GiftDto? Gift, bool Mine);
public sealed record ReadResult(DateTime ReadAt, GiftDto? Gift, bool GiftGranted);

public sealed class CreateInviteValidator : AbstractValidator<CreateInviteRequest>
{
    public CreateInviteValidator() => RuleFor(x => x.Kind).Must(k => RelationKinds.All.Contains(k)).WithMessage("請選擇「好友」或「情侶」。");
}

public sealed class CodeValidator : AbstractValidator<CodeRequest>
{
    public CodeValidator() => RuleFor(x => x.Code).NotEmpty().WithMessage("請輸入邀請碼。").MaximumLength(20).WithMessage("邀請碼太長了。");
}

public sealed class SendLetterValidator : AbstractValidator<SendLetterRequest>
{
    public SendLetterValidator()
    {
        RuleFor(x => x.RecipientUserId).NotEmpty().WithMessage("請選擇收信人。");
        RuleFor(x => x.Body).NotEmpty().WithMessage("信裡還沒有寫字喔。")
            .Must(b => LetterRules.Length(b) <= 500).WithMessage("信最多 500 個字。")
            .Must(LetterRules.TextAllowed).WithMessage("信裡有不能使用的字元。");
        RuleFor(x => x.PaperItemId).MaximumLength(80);
        RuleFor(x => x.GiftItemId).MaximumLength(80);
    }
}

public static class LetterRules
{
    public static int Length(string? s) => s is null ? 0 : new System.Globalization.StringInfo(s.Trim()).LengthInTextElements;

    /// <summary>Plain text: letters, punctuation, emoji, spaces and line breaks; no control characters. The apps render it as text, never as HTML.</summary>
    public static bool TextAllowed(string? s) => s is not null && s.All(c => !char.IsControl(c) || c is '\n' or '\r' or '\t');

    public static string Preview(string body)
    {
        var flat = string.Join(' ', body.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)).Trim();
        var e = System.Globalization.StringInfo.GetTextElementEnumerator(flat);
        var sb = new System.Text.StringBuilder();
        var n = 0;
        while (e.MoveNext() && n < 30) { sb.Append((string)e.Current); n++; }
        return n == 30 && flat.Length > sb.Length ? sb + "…" : sb.ToString();
    }
}
