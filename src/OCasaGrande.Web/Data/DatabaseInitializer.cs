using Microsoft.EntityFrameworkCore;

namespace OCasaGrande.Web.Data;

public static class DatabaseInitializer
{
    // EF Core takes a database-wide lock while migrating, so concurrent instances starting at once are safe.
    public static void Migrate(DatabaseConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection.Migrations)
            .Options;

        using var db = new AppDbContext(options);
        db.Database.Migrate();
    }
}
