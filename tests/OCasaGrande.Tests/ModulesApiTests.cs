using System.Net;
using System.Net.Http.Json;
using OCasaGrande.Web.Api.Modules;
using OCasaGrande.Web.Auth;

namespace OCasaGrande.Tests;

public sealed class ModulesApiTests(AppFactory factory) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task ListModules_RequiresAdmin()
    {
        var client = factory.CreateBrowserClient();

        var response = await client.GetAsync("/api/admin/modules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListModules_ReturnsSeededModule()
    {
        var client = await SignedInClientAsync();

        var modules = await client.GetFromJsonAsync<List<ModuleItem>>("/api/admin/modules");

        Assert.Contains(modules!, m => m.Key == "monitoramento");
    }

    [Fact]
    public async Task UpdateModule_WithoutCsrfToken_IsRejected()
    {
        var client = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/admin/modules/monitoramento", new { isEnabled = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateModule_TogglesAndPersists()
    {
        var client = await SignedInClientAsync();
        var token = await AppFactory.GetCsrfTokenAsync(client);

        var disable = await PutAsync(client, "/api/admin/modules/monitoramento", new { isEnabled = false }, token);
        var afterDisable = await client.GetFromJsonAsync<List<ModuleItem>>("/api/admin/modules");
        var enable = await PutAsync(client, "/api/admin/modules/monitoramento", new { isEnabled = true }, token);

        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
        Assert.False(afterDisable!.Single(m => m.Key == "monitoramento").IsEnabled);
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/modules/nao-existe", HttpStatusCode.NotFound)]
    [InlineData("/api/admin/modules/x'%20OR%201=1--", HttpStatusCode.NotFound)]
    public async Task UpdateModule_RejectsUnknownOrMalformedKeys(string path, HttpStatusCode expected)
    {
        var client = await SignedInClientAsync();
        var token = await AppFactory.GetCsrfTokenAsync(client);

        var response = await PutAsync(client, path, new { isEnabled = true }, token);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task UpdateModule_WithoutValue_ReturnsValidationProblem()
    {
        var client = await SignedInClientAsync();
        var token = await AppFactory.GetCsrfTokenAsync(client);

        var response = await PutAsync(client, "/api/admin/modules/monitoramento", new { }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = factory.CreateBrowserClient();
        var token = await AppFactory.GetCsrfTokenAsync(client);
        var login = await AppFactory.LoginAsync(client, AppFactory.AdminEmail, AppFactory.AdminPassword, token);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        return client;
    }

    private static async Task<HttpResponseMessage> PutAsync(HttpClient client, string path, object body, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(body) };
        request.Headers.Add(AdminAuthentication.CsrfHeaderName, token);
        return await client.SendAsync(request);
    }
}
