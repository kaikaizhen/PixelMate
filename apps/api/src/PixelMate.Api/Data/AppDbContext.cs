using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Pets;

namespace PixelMate.Api.Data;

/// <summary>EF Core context. Entities arrive feature by feature (pets/inventory/mail ...); all times are UTC.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<PetSkill> PetSkills => Set<PetSkill>();

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

        b.Entity<Pet>(e =>
        {
            e.ToTable("pets");
            e.HasKey(x => x.PetId);
            e.Property(x => x.Name).HasMaxLength(48).IsRequired();          // 12 characters, up to 4 UTF-8 bytes each
            e.Property(x => x.BodyVariant).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.OwnerId).IsUnique();                          // MVP: one main pet per owner
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Skills).WithOne().HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<PetSkill>(e =>
        {
            e.ToTable("pet_skills", t => t.HasCheckConstraint("ck_pet_skills_points", "points >= 0 AND points <= 5"));
            e.HasKey(x => new { x.PetId, x.SkillKey });
            e.Property(x => x.SkillKey).HasMaxLength(20);
        });
    }
}
