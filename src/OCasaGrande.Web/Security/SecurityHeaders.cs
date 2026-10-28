namespace OCasaGrande.Web.Security;

public static class SecurityHeaders
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; font-src 'self'; " +
        "connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // Authenticated pages and API responses must never be stored by browsers or shared caches.
            var path = context.Request.Path;
            if (path.StartsWithSegments("/api") || path.StartsWithSegments("/admin"))
            {
                headers.CacheControl = "no-store";
            }

            await next();
        });
}
