using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OCasaGrande.Web.Auth;
using OCasaGrande.Web.Data;
using OCasaGrande.Web.Security;

namespace OCasaGrande.Web.Api.SecurityEvents;

public static class SecurityEventEndpoints
{
    private const int RecentLimit = 50;

    public static IEndpointRouteBuilder MapSecurityEventsApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/security-events", ListRecent)
            .RequireAuthorization(AdminAuthentication.AdminPolicy)
            .RequireRateLimiting(RateLimiting.ApiPolicy);

        return app;
    }

    private static async Task<Ok<List<SecurityEventItem>>> ListRecent(AppDbContext db, CancellationToken cancellationToken)
    {
        var events = await db.SecurityEvents.AsNoTracking()
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Take(RecentLimit)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(events
            .Select(e => new SecurityEventItem(e.OccurredAt, e.Type.ToString(), e.IpAddress, e.Actor, e.Detail))
            .ToList());
    }
}
