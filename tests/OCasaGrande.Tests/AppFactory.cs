using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using OCasaGrande.Web.Auth;

namespace OCasaGrande.Tests;

public class AppFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@ocasagrande.test";
    public const string AdminPassword = "senha-de-teste-segura";

    private static readonly string AdminPasswordHash = AdminCredentialValidator.HashPassword(AdminPassword);

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"ocasagrande-tests-{Guid.NewGuid():N}.db");

    protected virtual int LoginPermitsPerMinute => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Database:Path", _databasePath);
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}

public sealed class StrictLoginLimitFactory : AppFactory
{
    public const int Limit = 3;

    protected override int LoginPermitsPerMinute => Limit;
}
