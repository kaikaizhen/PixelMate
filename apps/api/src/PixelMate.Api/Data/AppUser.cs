using Microsoft.AspNetCore.Identity;

namespace PixelMate.Api.Data;

/// <summary><c>users</c> table (docs/後端.md §2): Identity columns + display name + UTC creation time.</summary>
public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
