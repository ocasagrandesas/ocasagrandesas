using OCasaGrande.Web.Auth;

namespace OCasaGrande.Web.Site;

public static class PageEndpoints
{
    private const string HtmlContentType = "text/html; charset=utf-8";

    public static IEndpointRouteBuilder MapSitePages(this IEndpointRouteBuilder app)
    {
        var htmlRoot = Path.Combine(app.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "html");

        IResult Page(string relativePath) => TypedResults.PhysicalFile(Path.Combine(htmlRoot, relativePath), HtmlContentType);

        app.MapGet("/", () => Page("index.html"));
        app.MapGet("/entrar", () => Page("entrar.html"));
        app.MapGet(AdminAuthentication.LoginPagePath, () => Page(Path.Combine("admin", "entrar.html")));
        app.MapGet("/admin", () => Page(Path.Combine("admin", "configuracoes.html")))
            .RequireAuthorization(AdminAuthentication.AdminPolicy);

        return app;
    }
}
