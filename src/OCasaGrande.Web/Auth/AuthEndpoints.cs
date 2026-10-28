using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using OCasaGrande.Web.Domain;
using OCasaGrande.Web.Security;

namespace OCasaGrande.Web.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthApi(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").RequireRateLimiting(RateLimiting.ApiPolicy);

        auth.MapGet("/csrf", GetCsrfToken);
        auth.MapPost("/login", Login)
            .RequireRateLimiting(RateLimiting.LoginPolicy)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        auth.MapPost("/logout", Logout)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        auth.MapGet("/session", GetSession)
            .RequireAuthorization(AdminAuthentication.AdminPolicy);

        return app;
    }

    // The token is tied to the current identity, so the front-end must fetch a new one after logging in.
    private static Ok<CsrfTokenResponse> GetCsrfToken(IAntiforgery antiforgery, HttpContext context) =>
        TypedResults.Ok(new CsrfTokenResponse(antiforgery.GetAndStoreTokens(context).RequestToken!));

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> Login(
        LoginRequest request,
        AdminCredentialValidator validator,
        SharedRateLimiter sharedLimiter,
        SecurityAudit audit,
        HttpContext context,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!await sharedLimiter.TryAcquireLoginAsync(context, cancellationToken))
        {
            context.Response.Headers.RetryAfter = "60";
            return TypedResults.Problem(
                title: "Muitas tentativas. Aguarde um minuto e tente novamente.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (!validator.Validate(request.Email!, request.Password!))
        {
            loggerFactory.CreateLogger(nameof(AuthEndpoints)).LogWarning(
                "Tentativa de login de administrador recusada para o IP {RemoteIp}.", context.Connection.RemoteIpAddress);
            await audit.RecordAsync(SecurityEventType.LoginFailed, context, cancellationToken);

            return TypedResults.Problem(title: "E-mail ou senha inválidos.", statusCode: StatusCodes.Status401Unauthorized);
        }

        var principal = validator.CreatePrincipal();
        await audit.RecordAsync(
            SecurityEventType.LoginSucceeded, context, cancellationToken, principal.FindFirstValue(ClaimTypes.NameIdentifier));
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Logout(HttpContext context, SecurityAudit audit, CancellationToken cancellationToken)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await audit.RecordAsync(
                SecurityEventType.Logout, context, cancellationToken, context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }

    private static Ok<SessionResponse> GetSession(ClaimsPrincipal user) =>
        TypedResults.Ok(new SessionResponse(user.FindFirstValue(ClaimTypes.NameIdentifier)!));
}
