using System.Net;
using OCasaGrande.Web.Auth;

namespace OCasaGrande.Tests;

public sealed class AdminAuthTests(AppFactory factory) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task AdminPage_RedirectsAnonymousVisitorsToLogin()
    {
        var client = factory.CreateBrowserClient();

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AdminAuthentication.LoginPagePath, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_WithoutCsrfToken_IsRejected()
    {
        var client = factory.CreateBrowserClient();

        var response = await AppFactory.LoginAsync(client, AppFactory.AdminEmail, AppFactory.AdminPassword, csrfToken: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(AppFactory.AdminEmail, "senha-errada-123")]
    [InlineData("outra-pessoa@ocasagrande.test", AppFactory.AdminPassword)]
    public async Task Login_WithWrongCredentials_ReturnsUnauthorized(string email, string password)
    {
        var client = factory.CreateBrowserClient();
        var token = await AppFactory.GetCsrfTokenAsync(client);

        var response = await AppFactory.LoginAsync(client, email, password, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie") &&
                     response.Headers.GetValues("Set-Cookie").Any(c => c.StartsWith("ocg.session=")));
    }

    [Fact]
    public async Task Login_WithOversizedPassword_ReturnsValidationProblem()
    {
        var client = factory.CreateBrowserClient();
        var token = await AppFactory.GetCsrfTokenAsync(client);

        var response = await AppFactory.LoginAsync(
            client, AppFactory.AdminEmail, new string('a', AdminCredentialValidator.MaxPasswordLength + 1), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_OpensAdminPage()
    {
        var client = factory.CreateBrowserClient();
        var token = await AppFactory.GetCsrfTokenAsync(client);

        var login = await AppFactory.LoginAsync(client, AppFactory.AdminEmail, AppFactory.AdminPassword, token);
        var sessionCookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith("ocg.session="));
        var adminPage = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        Assert.Contains("httponly", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, adminPage.StatusCode);
    }
}

public sealed class LoginRateLimitTests(StrictLoginLimitFactory factory) : IClassFixture<StrictLoginLimitFactory>
{
    [Fact]
    public async Task Login_IsRateLimitedPerClient()
    {
        var client = factory.CreateBrowserClient();
        var token = await AppFactory.GetCsrfTokenAsync(client);

        for (var attempt = 0; attempt < StrictLoginLimitFactory.Limit; attempt++)
        {
            var allowed = await AppFactory.LoginAsync(client, AppFactory.AdminEmail, "senha-errada-123", token);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var blocked = await AppFactory.LoginAsync(client, AppFactory.AdminEmail, AppFactory.AdminPassword, token);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }
}
