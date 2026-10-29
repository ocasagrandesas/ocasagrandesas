using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using OCasaGrande.Web.Domain;

namespace OCasaGrande.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    // Supabase publishes every table in "public" through its REST Data API, so nothing may live there.
    public const string Schema = "ocasagrande";

    public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schema));

    public DbSet<PlatformModule> Modules => Set<PlatformModule>();

    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();

    public DbSet<RateLimitWindow> RateLimitWindows => Set<RateLimitWindow>();

    // Key ring that encrypts the session and CSRF cookies, shared by every instance.
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<PlatformModule>(module =>
        {
            module.ToTable("Modules");
            module.Property(m => m.Key).HasMaxLength(PlatformModule.KeyMaxLength);
            module.Property(m => m.Name).HasMaxLength(100);
            module.Property(m => m.Description).HasMaxLength(500);
            module.HasIndex(m => m.Key).IsUnique();
            module.HasData(ModuleSeed.Modules);
        });

        modelBuilder.Entity<SecurityEvent>(securityEvent =>
        {
            securityEvent.ToTable("SecurityEvents");
            securityEvent.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
            securityEvent.Property(e => e.IpAddress).HasMaxLength(SecurityEvent.IpAddressMaxLength);
            securityEvent.Property(e => e.Actor).HasMaxLength(SecurityEvent.ActorMaxLength);
            securityEvent.Property(e => e.Detail).HasMaxLength(SecurityEvent.DetailMaxLength);
            securityEvent.HasIndex(e => e.OccurredAt);
        });

        modelBuilder.Entity<RateLimitWindow>(window =>
        {
            window.ToTable("RateLimitWindows");
            window.HasKey(w => w.Key);
            window.Property(w => w.Key).HasMaxLength(RateLimitWindow.KeyMaxLength);
            window.HasIndex(w => w.WindowStart);
        });
    }
}
