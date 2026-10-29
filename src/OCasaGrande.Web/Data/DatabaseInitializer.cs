using Microsoft.EntityFrameworkCore;

namespace OCasaGrande.Web.Data;

public static class DatabaseInitializer
{
    // EF Core takes a database-wide lock while migrating, so concurrent instances starting at once are safe.
    public static void Migrate(DatabaseConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContext.Configure(options, connection.Migrations);

        using var db = new AppDbContext(options.Options);
        db.Database.Migrate();
    }
}
