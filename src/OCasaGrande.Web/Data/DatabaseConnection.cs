using Npgsql;

namespace OCasaGrande.Web.Data;

// PostgreSQL is the only database. Locally the connection comes from ConnectionStrings:Postgres (user-secrets);
// on Vercel the Supabase integration provides POSTGRES_URL (transaction pooler) and POSTGRES_URL_NON_POOLING.
public sealed record DatabaseConnection(string Application, string Migrations)
{
    public static DatabaseConnection Resolve(IConfiguration configuration)
    {
        if (configuration.GetConnectionString("Postgres") is { Length: > 0 } connectionString)
        {
            var normalized = Secure(new NpgsqlConnectionStringBuilder(connectionString));
            return new DatabaseConnection(normalized, normalized);
        }

        if (configuration["POSTGRES_URL"] is { Length: > 0 } pooledUrl)
        {
            // The transaction pooler hands the server connection to other clients after each transaction,
            // so the session-reset command Npgsql sends on close would run on someone else's session.
            var application = new NpgsqlConnectionStringBuilder(FromUrl(pooledUrl)) { NoResetOnClose = true }.ConnectionString;

            // Migrations need a session-level connection; the transaction pooler cannot hold one.
            var migrations = configuration["POSTGRES_URL_NON_POOLING"] is { Length: > 0 } directUrl
                ? FromUrl(directUrl)
                : throw new InvalidOperationException("Defina POSTGRES_URL_NON_POOLING para as migrations.");

            return new DatabaseConnection(application, migrations);
        }

        throw new InvalidOperationException(
            "Nenhuma conexão PostgreSQL configurada. Defina ConnectionStrings:Postgres ou POSTGRES_URL.");
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
    // Supabase certificates are signed by Supabase's own CA, which PGSSLROOTCERT points to in the container.
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
