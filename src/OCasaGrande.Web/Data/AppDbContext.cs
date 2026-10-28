using Microsoft.EntityFrameworkCore;
using OCasaGrande.Web.Domain;

namespace OCasaGrande.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlatformModule> Modules => Set<PlatformModule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlatformModule>(module =>
        {
            module.ToTable("Modules");
            module.Property(m => m.Key).HasMaxLength(PlatformModule.KeyMaxLength);
            module.Property(m => m.Name).HasMaxLength(100);
            module.Property(m => m.Description).HasMaxLength(500);
            module.HasIndex(m => m.Key).IsUnique();
            module.HasData(ModuleSeed.Modules);
        });
    }
}
