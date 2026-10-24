using Microsoft.EntityFrameworkCore;

namespace PixelMate.Api.Data;

/// <summary>EF Core context. Entities arrive feature by feature (users/pets/inventory/mail ...); all times are UTC.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
