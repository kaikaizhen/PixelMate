using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PixelMate.Api.Data;

/// <summary>EF Core context. Entities arrive feature by feature (pets/inventory/mail ...); all times are UTC.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // snake_case tables named like docs/後端.md
        b.Entity<AppUser>(e =>
        {
            e.ToTable("users");
            e.Property(x => x.Id).HasColumnName("user_id");
            e.Property(x => x.DisplayName).HasMaxLength(40).IsRequired();
        });
        b.Entity<IdentityRole<Guid>>().ToTable("roles");
        b.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        b.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        b.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        b.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        b.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        b.Entity<DataProtectionKey>().ToTable("data_protection_keys");
    }
}
