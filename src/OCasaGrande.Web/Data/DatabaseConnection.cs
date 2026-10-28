using Npgsql;

namespace OCasaGrande.Web.Data;

// PostgreSQL is the only database. Locally the connection comes from ConnectionStrings:Postgres (user-secrets);
// on Vercel the Neon integration provides DATABASE_URL (pooled) and DATABASE_URL_UNPOOLED as postgresql:// URLs.
public sealed record DatabaseConnection(string Application, string Migrations)
{
    public static DatabaseConnection Resolve(IConfiguration configuration)
    {
        if (configuration.GetConnectionString("Postgres") is { Length: > 0 } connectionString)
        {
            var normalized = Secure(new NpgsqlConnectionStringBuilder(connectionString));
            return new DatabaseConnection(normalized, normalized);
        }

        if (configuration["DATABASE_URL"] is { Length: > 0 } pooledUrl)
        {
            var application = FromUrl(pooledUrl);
            var migrations = configuration["DATABASE_URL_UNPOOLED"] is { Length: > 0 } directUrl ? FromUrl(directUrl) : application;
            return new DatabaseConnection(application, migrations);
        }

        throw new InvalidOperationException(
            "Nenhuma conexão PostgreSQL configurada. Defina ConnectionStrings:Postgres ou DATABASE_URL.");
    }

    // Npgsql only understands keyword=value connection strings, not postgresql:// URLs.
    public static string FromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgres" or "postgresql"))
        {
            throw new InvalidOperationException("A URL do banco precisa começar com postgresql://.");
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
        };

        return Secure(builder);
    }

    // Remote servers must use TLS with full certificate and host-name validation; only a local server may skip it.
    private static string Secure(NpgsqlConnectionStringBuilder builder)
    {
        if (!IsLocalHost(builder.Host))
        {
            builder.SslMode = SslMode.VerifyFull;
        }

        builder.IncludeErrorDetail = false;
        return builder.ConnectionString;
    }

    private static bool IsLocalHost(string? host) =>
        host is "localhost" or "127.0.0.1" or "::1";
}
