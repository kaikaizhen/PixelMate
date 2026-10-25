using FluentValidation;

namespace PixelMate.Api.Devices;

/// <summary><c>devices</c>: a physical companion (ESP32) bound to one pet. The credential itself is never stored, only its SHA-256.</summary>
public class Device
{
    public Guid DeviceId { get; set; } = Guid.NewGuid();
    public Guid PetId { get; set; }
    public Guid OwnerId { get; set; }
    public string Model { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Null until the device has collected its credential (the owner approved the pairing, the device has not come for it yet).</summary>
    public string? CredentialHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string State => RevokedAt is not null ? "revoked" : ActivatedAt is null ? "waiting" : "active";
}

/// <summary><c>device_pairings</c>: a device asked to be paired. It shows the code; the owner types it into the app; the device then collects its credential once.</summary>
public class DevicePairing
{
    public Guid PairingId { get; set; } = Guid.NewGuid();
    public string CodeHash { get; set; } = "";
    public string PollTokenHash { get; set; } = "";
    public string Model { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? DeviceId { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public sealed class DeviceOptions
{
    public int PairingMinutes { get; set; } = 10;
    public int MaxDevicesPerPet { get; set; } = 3;
    public int PollIntervalSeconds { get; set; } = 3;
    /// <summary>LastSeenAt is only rewritten when it is older than this, so a device polling every few seconds does not write every time.</summary>
    public int LastSeenGraceSeconds { get; set; } = 60;
}

public sealed record StartPairingRequest(string Model);
public sealed record PollPairingRequest(Guid PairingId, string PollToken);
public sealed record ApprovePairingRequest(string Code, string? Name);
public sealed record RenameDeviceRequest(string Name);
public sealed record PairingStarted(Guid PairingId, string Code, string PollToken, DateTime ExpiresAt, int PollIntervalSeconds);
public sealed record PairingStatus(string Status, Guid? DeviceId = null, string? Credential = null, Guid? PetId = null);
public sealed record DeviceDto(Guid DeviceId, Guid PetId, string Model, string Name, string State, DateTime CreatedAt, DateTime? ActivatedAt, DateTime? LastSeenAt);

public sealed class StartPairingValidator : AbstractValidator<StartPairingRequest>
{
    public StartPairingValidator() => RuleFor(x => x.Model).NotEmpty().MaximumLength(40).Matches("^[A-Za-z0-9 ._-]+$").WithMessage("裝置型號只能用英數字、空格和 . _ -。");
}

public sealed class PollPairingValidator : AbstractValidator<PollPairingRequest>
{
    public PollPairingValidator()
    {
        RuleFor(x => x.PairingId).NotEmpty();
        RuleFor(x => x.PollToken).NotEmpty().MaximumLength(100);
    }
}

public sealed class ApprovePairingValidator : AbstractValidator<ApprovePairingRequest>
{
    public ApprovePairingValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("請輸入裝置畫面上的配對碼。").MaximumLength(20);
        RuleFor(x => x.Name).MaximumLength(30).WithMessage("名稱最多 30 個字。");
    }
}

public sealed class RenameDeviceValidator : AbstractValidator<RenameDeviceRequest>
{
    public RenameDeviceValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(30);
}
