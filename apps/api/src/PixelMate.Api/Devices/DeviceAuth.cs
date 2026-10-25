using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PixelMate.Api.Data;

namespace PixelMate.Api.Devices;

public static class DeviceCredentials
{
    public const string Scheme = "Device";

    /// <summary>"{deviceId}.{32 random bytes, base64url}": the id finds the row in one lookup, the secret is only ever compared as a hash.</summary>
    public static string New(Guid deviceId) => $"{deviceId:N}.{Base64Url(RandomNumberGenerator.GetBytes(32))}";

    public static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    public static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static bool TryParse(string? bearer, out Guid deviceId)
    {
        deviceId = Guid.Empty;
        if (string.IsNullOrEmpty(bearer)) return false;
        var dot = bearer.IndexOf('.');
        return dot == 32 && Guid.TryParseExact(bearer[..32], "N", out deviceId);
    }

    public static bool Matches(string? storedHash, string presented) =>
        storedHash is not null && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(storedHash), Encoding.ASCII.GetBytes(Hash(presented)));
}

/// <summary>
/// Bearer authentication for devices. The result is a principal that carries the OWNER's user id (so the same read endpoints work) plus the
/// device and pet ids. It only counts on the /device/* routes: those are the only ones that accept this scheme.
/// </summary>
public sealed class DeviceAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, AppDbContext db, TimeProvider clock, IOptions<DeviceOptions> dev)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        var token = header["Bearer ".Length..].Trim();
        if (!DeviceCredentials.TryParse(token, out var id)) return AuthenticateResult.Fail("bad credential");

        var d = await db.Devices.AsNoTracking().FirstOrDefaultAsync(x => x.DeviceId == id, Context.RequestAborted);
        if (d is null || d.RevokedAt is not null || !DeviceCredentials.Matches(d.CredentialHash, token)) return AuthenticateResult.Fail("bad credential");

        var now = clock.GetUtcNow().UtcDateTime;
        if (d.LastSeenAt is null || (now - d.LastSeenAt.Value).TotalSeconds > dev.Value.LastSeenGraceSeconds)
            await db.Devices.Where(x => x.DeviceId == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAt, now), Context.RequestAborted);

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, d.OwnerId.ToString()),
            new Claim("device_id", d.DeviceId.ToString()),
            new Claim("pet_id", d.PetId.ToString()),
        ], DeviceCredentials.Scheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), DeviceCredentials.Scheme));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        return Task.CompletedTask;
    }
}
