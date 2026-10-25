using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PixelMate.Api.Items;
using PixelMate.Api.Life;
using PixelMate.Api.Pets;
using PixelMate.Api.Realtime;
using PixelMate.Api.Rooms;
using PixelMate.Api.Social;

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
    public DbSet<PetLife> PetLives => Set<PetLife>();
    public DbSet<PetEvent> PetEvents => Set<PetEvent>();
    public DbSet<PairingInvite> PairingInvites => Set<PairingInvite>();
    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<Letter> Letters => Set<Letter>();
    public DbSet<RoomState> RoomStates => Set<RoomState>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<RoomPlacement> RoomPlacements => Set<RoomPlacement>();

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
        b.Entity<PetLife>(e =>
        {
            e.ToTable("pet_life");
            e.HasKey(x => x.PetId);
            e.Property(x => x.CursorSlot).IsConcurrencyToken();
            e.HasOne<Pet>().WithOne().HasForeignKey<PetLife>(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<PetEvent>(e =>
        {
            e.ToTable("pet_events");
            e.HasKey(x => x.EventId);
            e.Property(x => x.Kind).HasMaxLength(20).IsRequired();
            e.Property(x => x.ItemId).HasMaxLength(80).IsRequired();
            e.HasIndex(x => new { x.PetId, x.Slot }).IsUnique();             // one event per pet and slot, however often it is triggered
            e.HasIndex(x => new { x.PetId, x.OccurredAt });
            e.HasOne<Pet>().WithMany().HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ItemDefinition>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<PairingInvite>(e =>
        {
            e.ToTable("pairing_invites");
            e.HasKey(x => x.InviteId);
            e.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.Kind).HasMaxLength(12).IsRequired();
            e.HasIndex(x => x.CodeHash).IsUnique();
            e.HasIndex(x => x.InviterId);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.InviterId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Relationship>(e =>
        {
            e.ToTable("relationships", t => t.HasCheckConstraint("ck_relationships_order", "user_low_id < user_high_id"));
            e.HasKey(x => x.RelationshipId);
            e.Property(x => x.Kind).HasMaxLength(12).IsRequired();
            e.HasIndex(x => new { x.UserLowId, x.UserHighId }).IsUnique();   // a pair of players exists once (ending keeps the row)
            e.HasIndex(x => x.UserHighId);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserLowId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserHighId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Letter>(e =>
        {
            e.ToTable("letters");
            e.HasKey(x => x.LetterId);
            e.Property(x => x.Body).HasMaxLength(2400).IsRequired();          // 500 characters, some of them emoji
            e.Property(x => x.PaperItemId).HasMaxLength(80);
            e.Property(x => x.GiftItemId).HasMaxLength(80);
            e.Property(x => x.IdempotencyKey).HasMaxLength(64).IsRequired();
            e.HasIndex(x => new { x.SenderId, x.IdempotencyKey }).IsUnique();  // a retried send is the same letter
            e.HasIndex(x => new { x.RecipientId, x.DeliverAt });
            e.HasIndex(x => new { x.SenderId, x.SentAt });
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ItemDefinition>().WithMany().HasForeignKey(x => x.GiftItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ItemDefinition>().WithMany().HasForeignKey(x => x.PaperItemId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<RoomState>(e =>
        {
            e.ToTable("room_states");
            e.HasKey(x => x.OwnerId);
            e.Property(x => x.FloorItemId).HasMaxLength(80);
            e.HasOne<AppUser>().WithOne().HasForeignKey<RoomState>(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ItemDefinition>().WithMany().HasForeignKey(x => x.FloorItemId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<RoomPlacement>(e =>
        {
            e.ToTable("room_placements", t => t.HasCheckConstraint("ck_room_placements_xy", "x >= 0 AND x <= 240 AND y >= 0 AND y <= 240"));
            e.HasKey(x => x.PlacementId);
            e.Property(x => x.ItemId).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.OwnerId);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ItemDefinition>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<OutboxEvent>(e =>
        {
            e.ToTable("outbox_events");
            e.HasKey(x => x.Seq);
            e.Property(x => x.Seq).ValueGeneratedOnAdd();
            e.Property(x => x.Type).HasMaxLength(40).IsRequired();
            e.Property(x => x.PayloadJson).HasMaxLength(1000).IsRequired();
            e.HasIndex(x => x.EventId).IsUnique();
            e.HasIndex(x => new { x.UserId, x.Seq });
            e.HasIndex(x => x.DispatchedAt);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
