using Microsoft.Extensions.Configuration;
using Npgsql;
using OCasaGrande.Web.Data;

namespace OCasaGrande.Tests;

public sealed class DatabaseConnectionTests
{
    private const string PoolerUrl =
        "postgresql://postgres.abcdefghijklmnop:p%40ss%3Bword@aws-0-us-east-1.pooler.supabase.com:6543/postgres?sslmode=require&supa=base-pooler.x";

    private const string DirectUrl =
        "postgresql://postgres.abcdefghijklmnop:p%40ss%3Bword@aws-0-us-east-1.pooler.supabase.com:5432/postgres?sslmode=require";

    [Fact]
    public void FromUrl_ConvertsSupabaseUrlAndRequiresVerifiedTls()
    {
        var connection = new NpgsqlConnectionStringBuilder(DatabaseConnection.FromUrl(PoolerUrl));

        Assert.Equal("aws-0-us-east-1.pooler.supabase.com", connection.Host);
        Assert.Equal(6543, connection.Port);
        Assert.Equal("postgres", connection.Database);
        Assert.Equal("postgres.abcdefghijklmnop", connection.Username);
        Assert.Equal("p@ss;word", connection.Password);
        Assert.Equal(SslMode.VerifyFull, connection.SslMode);
    }

    [Theory]
    [InlineData("mysql://user:pass@host/db")]
    [InlineData("not a url")]
    public void FromUrl_RejectsNonPostgresUrls(string url)
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseConnection.FromUrl(url));
    }

    [Fact]
    public void Resolve_UsesPoolerForQueriesAndDirectConnectionForMigrations()
    {
        var connection = DatabaseConnection.Resolve(Configuration(new()
        {
            ["POSTGRES_URL"] = PoolerUrl,
            ["POSTGRES_URL_NON_POOLING"] = DirectUrl,
        }));

        var application = new NpgsqlConnectionStringBuilder(connection.Application);
        Assert.Equal(6543, application.Port);
        Assert.True(application.NoResetOnClose);

        var migrations = new NpgsqlConnectionStringBuilder(connection.Migrations);
        Assert.Equal(5432, migrations.Port);
        Assert.False(migrations.NoResetOnClose);
    }

    [Fact]
    public void Resolve_RefusesToMigrateThroughTheTransactionPooler()
    {
        var configuration = Configuration(new() { ["POSTGRES_URL"] = PoolerUrl });

        Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Resolve(configuration));
    }

    [Fact]
    public void Resolve_FailsWhenNoPostgresIsConfigured()
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Resolve(Configuration(new())));
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
