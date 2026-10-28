using OCasaGrande.Web.Data;
using OCasaGrande.Web.Domain;

namespace OCasaGrande.Web.Security;

public sealed class SecurityAudit(AppDbContext db)
{
    // Adds the event to the current unit of work; it is saved together with the change it describes.
    public void Add(SecurityEventType type, HttpContext context, string? actor = null, string? detail = null)
    {
        db.SecurityEvents.Add(new SecurityEvent
        {
            OccurredAt = DateTimeOffset.UtcNow,
            Type = type,
            IpAddress = Truncate(context.Connection.RemoteIpAddress?.ToString(), SecurityEvent.IpAddressMaxLength),
            Actor = Truncate(actor, SecurityEvent.ActorMaxLength),
            Detail = Truncate(detail, SecurityEvent.DetailMaxLength),
        });
    }

    public async Task RecordAsync(
        SecurityEventType type, HttpContext context, CancellationToken cancellationToken, string? actor = null, string? detail = null)
    {
        Add(type, context, actor, detail);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is { Length: var length } && length > maxLength ? value[..maxLength] : value;
}
