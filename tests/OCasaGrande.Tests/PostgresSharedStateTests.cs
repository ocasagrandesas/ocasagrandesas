using System.Net;
using System.Net.Http.Json;
using OCasaGrande.Web.Api.SecurityEvents;
using OCasaGrande.Web.Auth;

namespace OCasaGrande.Tests;

// Two app instances on the same PostgreSQL database, as on Vercel when more than one container is running.
public sealed class PostgresSharedStateTests : IAsyncLifetime
{
    private readonly StrictLoginLimitFactory _first = new();
    private AppFactory _second = null!;

    public Task InitializeAsync()
    {
        _second = AppFactory.SharingDatabaseWith(_first);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task LoginLimit_IsSharedAcrossInstances()
    {
        var first = _first.CreateBrowserClient();
        var token = await AppFactory.GetCsrfTokenAsync(first);
        for (var attempt = 0; attempt < StrictLoginLimitFactory.Limit; attempt++)
        {
            await AppFactory.LoginAsync(first, AppFactory.AdminEmail, "senha-errada-123", token);
        }

        // The second instance's in-memory limiter has seen no requests, so a 429 here can only come from PostgreSQL.
        var second = _second.CreateBrowserClient();
        var secondToken = await AppFactory.GetCsrfTokenAsync(second);
        var blocked = await AppFactory.LoginAsync(second, AppFactory.AdminEmail, AppFactory.AdminPassword, secondToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task SessionCookie_IsAcceptedByAnotherInstance()
    {
        var first = _first.CreateBrowserClient();
        var token = await AppFactory.GetCsrfTokenAsync(first);
        var login = await AppFactory.LoginAsync(first, AppFactory.AdminEmail, AppFactory.AdminPassword, token);
        var sessionCookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("ocg.session=")).Split(';')[0];

        var second = _second.CreateBrowserClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/session");
        request.Headers.Add("Cookie", sessionCookie);
        var response = await second.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await _first.QueryScalarAsync<long>("""SELECT COUNT(*) FROM "DataProtectionKeys" """) > 0);
    }

    [Fact]
    public async Task FailedAndSuccessfulLogins_AreAudited_WithoutSecrets()
    {
        var client = _second.CreateBrowserClient();
        var token = await AppFactory.GetCsrfTokenAsync(client);
        await AppFactory.LoginAsync(client, "invasor@exemplo.test", "senha-errada-123", token);
        await AppFactory.LoginAsync(client, AppFactory.AdminEmail, AppFactory.AdminPassword, token);

        var events = await client.GetFromJsonAsync<List<SecurityEventItem>>("/api/admin/security-events");
        var stored = await _first.QueryScalarAsync<long>("""
            SELECT COUNT(*) FROM "SecurityEvents"
            WHERE "Actor" LIKE '%invasor%' OR "Detail" LIKE '%senha%'
            """);

        Assert.Contains(events!, e => e.Type == "LoginFailed" && e.Actor is null);
        Assert.Contains(events!, e => e.Type == "LoginSucceeded" && e.Actor == AppFactory.AdminEmail);
        Assert.Equal(0, stored);
    }

    public async Task DisposeAsync()
    {
        await _second.DisposeAsync();
        await _first.DisposeAsync();
    }
}
