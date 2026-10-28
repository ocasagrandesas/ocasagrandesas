using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace OCasaGrande.Web.Auth;

public static class AdminAuthentication
{
    public const string AdminPolicy = "admin";
    public const string AdminRole = "Admin";
    public const string LoginPagePath = "/admin/entrar";
    public const string CsrfHeaderName = "X-CSRF-TOKEN";

    public static IServiceCollection AddAdminAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<AdminAccountOptions>()
            .Bind(configuration.GetSection(AdminAccountOptions.SectionName))
            .Validate(
                options => string.IsNullOrEmpty(options.PasswordHash) || AdminCredentialValidator.IsValidHash(options.PasswordHash),
                "Admin:PasswordHash inválido. Gere o valor com o comando 'hash-password'.")
            .ValidateOnStart();
        services.AddSingleton<AdminCredentialValidator>();

        // Browsers treat localhost as a secure context, but the Development server may run over plain HTTP.
        var securePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "ocg.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = securePolicy;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
                options.LoginPath = LoginPagePath;
                options.AccessDeniedPath = LoginPagePath;

                options.Events.OnRedirectToLogin = context => RespondOrRedirect(context, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context => RespondOrRedirect(context, StatusCodes.Status403Forbidden);
                options.Events.OnValidatePrincipal = async context =>
                {
                    var validator = context.HttpContext.RequestServices.GetRequiredService<AdminCredentialValidator>();
                    if (!validator.IsCurrentSession(context.Principal))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, policy => policy.RequireAuthenticatedUser().RequireRole(AdminRole));

        services.AddAntiforgery(options =>
        {
            options.HeaderName = CsrfHeaderName;
            options.Cookie.Name = "ocg.csrf";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = securePolicy;
            options.SuppressXFrameOptionsHeader = true;
        });

        return services;
    }

    // API callers get a status code; page requests are redirected to the login page.
    private static Task RespondOrRedirect(RedirectContext<CookieAuthenticationOptions> context, int apiStatusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = apiStatusCode;
        }
        else
        {
            context.Response.Redirect(LoginPagePath);
        }

        return Task.CompletedTask;
    }
}
