using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Items;
using PixelMate.Api.Pets;

namespace PixelMate.Api.Data;

/// <summary>EF Core context. Entities arrive feature by feature (pets/inventory/mail ...); all times are UTC.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<PetSkill> PetSkills => Set<PetSkill>();
    public DbSet<ItemDefinition> ItemDefinitions => Set<ItemDefinition>();
    public DbSet<InventoryEntry> InventoryEntries => Set<InventoryEntry>();
    public DbSet<PetEquipment> PetEquipment => Set<PetEquipment>();

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
            e.Property(x => x.Revision).IsConcurrencyToken();                // two writers with the same revision: only one wins
            e.HasIndex(x => x.OwnerId).IsUnique();                          // MVP: one main pet per owner
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Skills).WithOne().HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Equipment).WithOne().HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<PetSkill>(e =>
        {
            e.ToTable("pet_skills", t => t.HasCheckConstraint("ck_pet_skills_points", "points >= 0 AND points <= 5"));
            e.HasKey(x => new { x.PetId, x.SkillKey });
            e.Property(x => x.SkillKey).HasMaxLength(20);
        });

        b.Entity<ItemDefinition>(e =>
        {
            e.ToTable("item_definitions");
            e.HasKey(x => x.ItemId);
            e.Property(x => x.ItemId).HasMaxLength(80);
            e.Property(x => x.AssetKey).HasMaxLength(80).IsRequired();
            e.Property(x => x.Category).HasMaxLength(20).IsRequired();
            e.Property(x => x.Rarity).HasMaxLength(20).IsRequired();
            e.Property(x => x.Slot).HasMaxLength(20);
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Batch).HasMaxLength(8);
            e.Property(x => x.DesignId).HasMaxLength(12);
            e.HasIndex(x => new { x.Category, x.Rarity });
        });
        b.Entity<InventoryEntry>(e =>
        {
            e.ToTable("inventory_entries", t => t.HasCheckConstraint("ck_inventory_quantity", "quantity >= 0"));
            e.HasKey(x => x.InventoryId);
            e.Property(x => x.ItemId).HasMaxLength(80).IsRequired();
            e.HasIndex(x => new { x.OwnerId, x.ItemId }).IsUnique();         // one stacked row per owner and item
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ItemDefinition>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<PetEquipment>(e =>
        {
            e.ToTable("pet_equipment");
            e.HasKey(x => new { x.PetId, x.Slot });                          // a slot holds at most one item
            e.Property(x => x.Slot).HasMaxLength(20);
            e.Property(x => x.ItemId).HasMaxLength(80).IsRequired();
            e.HasOne<InventoryEntry>().WithMany().HasForeignKey(x => x.InventoryId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
