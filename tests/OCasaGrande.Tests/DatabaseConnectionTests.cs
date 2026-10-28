using Microsoft.Extensions.Configuration;
using Npgsql;
using OCasaGrande.Web.Data;

namespace OCasaGrande.Tests;

public sealed class DatabaseConnectionTests
{
    [Fact]
    public void FromUrl_ConvertsNeonUrlAndRequiresVerifiedTls()
    {
        var connection = new NpgsqlConnectionStringBuilder(DatabaseConnection.FromUrl(
            "postgresql://app_user:p%40ss%3Bword@ep-test-pooler.sa-east-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require"));

        Assert.Equal("ep-test-pooler.sa-east-1.aws.neon.tech", connection.Host);
        Assert.Equal(5432, connection.Port);
        Assert.Equal("neondb", connection.Database);
        Assert.Equal("app_user", connection.Username);
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
    public void Resolve_UsesUnpooledUrlForMigrations()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "postgresql://u:p@ep-x-pooler.neon.tech/db",
            ["DATABASE_URL_UNPOOLED"] = "postgresql://u:p@ep-x.neon.tech/db",
        }).Build();

        var connection = DatabaseConnection.Resolve(configuration);

        Assert.Equal("ep-x-pooler.neon.tech", new NpgsqlConnectionStringBuilder(connection.Application).Host);
        Assert.Equal("ep-x.neon.tech", new NpgsqlConnectionStringBuilder(connection.Migrations).Host);
    }

    [Fact]
    public void Resolve_FailsWhenNoPostgresIsConfigured()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Resolve(configuration));
    }
}
