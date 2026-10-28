namespace OCasaGrande.Web.Api.SecurityEvents;

public sealed record SecurityEventItem(DateTimeOffset OccurredAt, string Type, string? IpAddress, string? Actor, string? Detail);
