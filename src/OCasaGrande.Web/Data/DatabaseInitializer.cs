using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace OCasaGrande.Web.Data;

public static class DatabaseInitializer
{
    public static string ResolvePath(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredPath = configuration["Database:Path"] ?? Path.Combine("data", "ocasagrande.db");
        return Path.GetFullPath(configuredPath, environment.ContentRootPath);
    }

    public static string ConnectionString(string databasePath) =>
        new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();

    public static void Migrate(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(ConnectionString(databasePath))
            .Options;

        using var db = new AppDbContext(options);
        db.Database.Migrate();
    }
}
