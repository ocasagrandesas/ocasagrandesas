using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace OCasaGrande.Web.Security;

public static class RateLimiting
{
    public const string LoginPolicy = "login";
    public const string ApiPolicy = "api";

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var globalPermits = configuration.GetValue("RateLimiting:GlobalPermitsPerMinute", 300);
        var apiPermits = configuration.GetValue("RateLimiting:ApiPermitsPerMinute", 60);
        var loginPermits = configuration.GetValue("RateLimiting:LoginPermitsPerMinute", 5);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }
                return ValueTask.CompletedTask;
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                PerClientPerMinute(context, globalPermits));
            options.AddPolicy(ApiPolicy, context => PerClientPerMinute(context, apiPermits));
            options.AddPolicy(LoginPolicy, context => PerClientPerMinute(context, loginPermits));
        });
    }

    // Counters live in memory, so each running instance limits on its own; the Vercel Firewall rule is the shared layer.
    private static RateLimitPartition<string> PerClientPerMinute(HttpContext context, int permits) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
}
