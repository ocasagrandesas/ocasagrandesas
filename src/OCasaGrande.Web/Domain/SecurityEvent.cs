namespace OCasaGrande.Web.Domain;

// Audit trail of security-relevant actions. Never stores passwords, hashes, tokens or the e-mail typed in a failed login.
public sealed class SecurityEvent
{
    public const int IpAddressMaxLength = 45;
    public const int ActorMaxLength = 254;
    public const int DetailMaxLength = 200;

    public long Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public SecurityEventType Type { get; set; }

    public string? IpAddress { get; set; }

    public string? Actor { get; set; }

    public string? Detail { get; set; }
}

public enum SecurityEventType
{
    LoginSucceeded = 1,
    LoginFailed = 2,
    Logout = 3,
    ModuleUpdated = 4,
}
