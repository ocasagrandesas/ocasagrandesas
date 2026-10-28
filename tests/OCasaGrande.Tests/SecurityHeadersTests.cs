using System.Net;

namespace OCasaGrande.Tests;

public sealed class SecurityHeadersTests(AppFactory factory) : IClassFixture<AppFactory>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/entrar")]
    [InlineData("/admin/entrar")]
    public async Task Pages_AreServedWithSecurityHeaders(string path)
    {
        var client = factory.CreateBrowserClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("script-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
    }

    [Theory]
    [InlineData("/html/index.html")]
    [InlineData("/html/admin/configuracoes.html")]
    [InlineData("/appsettings.json")]
    public async Task SourceFiles_AreNotPubliclyServed(string path)
    {
        var client = factory.CreateBrowserClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApiResponses_AreNotCached()
    {
        var client = factory.CreateBrowserClient();

        var response = await client.GetAsync("/api/auth/csrf");

        Assert.True(response.Headers.CacheControl?.NoStore);
    }
}
