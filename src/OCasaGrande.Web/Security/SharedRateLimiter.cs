using Microsoft.EntityFrameworkCore;
using OCasaGrande.Web.Data;
using OCasaGrande.Web.Domain;

namespace OCasaGrande.Web.Security;

// Fixed-window limiter stored in PostgreSQL, so the limit is shared by every instance. It runs after the
// in-memory limiter, which absorbs floods before they reach the database.
public sealed class SharedRateLimiter(AppDbContext db, IConfiguration configuration)
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Retention = TimeSpan.FromHours(1);

    public int LoginPermitsPerMinute { get; } = configuration.GetValue("RateLimiting:LoginPermitsPerMinute", 5);

    public async Task<bool> TryAcquireLoginAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"login:{client}";
        if (key.Length > RateLimitWindow.KeyMaxLength)
        {
            key = key[..RateLimitWindow.KeyMaxLength];
        }

        var now = DateTimeOffset.UtcNow;
        var windowStart = new DateTimeOffset(now.Ticks - now.Ticks % Window.Ticks, TimeSpan.Zero);
        var cutoff = now - Retention;

        // Interpolated values become SQL parameters; the upsert is atomic, so concurrent requests cannot both slip through.
        var count = await db.Database.SqlQuery<int>($"""
            INSERT INTO "RateLimitWindows" ("Key", "WindowStart", "Count")
            VALUES ({key}, {windowStart}, 1)
            ON CONFLICT ("Key") DO UPDATE SET
                "Count" = CASE WHEN "RateLimitWindows"."WindowStart" = EXCLUDED."WindowStart"
                               THEN "RateLimitWindows"."Count" + 1 ELSE 1 END,
                "WindowStart" = EXCLUDED."WindowStart"
            RETURNING "Count" AS "Value"
            """).ToListAsync(cancellationToken);

        await db.RateLimitWindows.Where(w => w.WindowStart < cutoff).ExecuteDeleteAsync(cancellationToken);

        return count.Single() <= LoginPermitsPerMinute;
    }
}
