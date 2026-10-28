using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OCasaGrande.Web.Auth;

namespace OCasaGrande.Tests;

// Each factory gets its own PostgreSQL database, created by the app's migrations and dropped on dispose.
// Two factories built with the same database name behave like two instances of the deployed app.
public class AppFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@ocasagrande.test";
    public const string AdminPassword = "senha-de-teste-segura";

    private static readonly string AdminPasswordHash = AdminCredentialValidator.HashPassword(AdminPassword);

    private readonly bool _ownsDatabase;

    public AppFactory() : this(loginPermitsPerMinute: 1000)
    {
    }

    protected AppFactory(int loginPermitsPerMinute) : this(NewDatabaseName(), ownsDatabase: true, loginPermitsPerMinute)
    {
    }

    private AppFactory(string databaseName, bool ownsDatabase, int loginPermitsPerMinute)
    {
        DatabaseName = databaseName;
        _ownsDatabase = ownsDatabase;
        LoginPermitsPerMinute = loginPermitsPerMinute;
        ConnectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString) { Database = databaseName }.ConnectionString;
    }

    public string DatabaseName { get; }

    public string ConnectionString { get; }

    public int LoginPermitsPerMinute { get; }

    // Local server credentials come from the web project's user-secrets (ConnectionStrings:Postgres) or OCG_TEST_POSTGRES.
    private static string BaseConnectionString =>
        new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build() is var configuration &&
        (configuration["OCG_TEST_POSTGRES"] ?? configuration.GetConnectionString("Postgres")) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                "Configure ConnectionStrings:Postgres nos user-secrets do OCasaGrande.Web para rodar os testes.");

    public static AppFactory SharingDatabaseWith(AppFactory other) =>
        new(other.DatabaseName, ownsDatabase: false, other.LoginPermitsPerMinute);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:PasswordHash", AdminPasswordHash);
        builder.UseSetting("RateLimiting:GlobalPermitsPerMinute", "1000");
        builder.UseSetting("RateLimiting:ApiPermitsPerMinute", "1000");
        builder.UseSetting("RateLimiting:LoginPermitsPerMinute", LoginPermitsPerMinute.ToString());
    }

    // HTTPS is required because production cookies are marked Secure.
    public HttpClient CreateBrowserClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<CsrfTokenResponse>("/api/auth/csrf");
        return response!.Token;
    }

    public static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password, string? csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password }),
        };
        if (csrfToken is not null)
        {
            request.Headers.Add(AdminAuthentication.CsrfHeaderName, csrfToken);
        }

        return await client.SendAsync(request);
    }

    public async Task<T> QueryScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();

        if (_ownsDatabase)
        {
            await using var connection = new NpgsqlConnection(BaseConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE)", connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static string NewDatabaseName() => $"ocasagrande_test_{Guid.NewGuid():N}";
}

public sealed class StrictLoginLimitFactory() : AppFactory(Limit)
{
    public const int Limit = 3;
}
