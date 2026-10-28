namespace OCasaGrande.Web.Domain;

// A feature area of the platform. Key is the stable identifier that future plans will reference to grant access.
public sealed class PlatformModule
{
    public const int KeyMaxLength = 50;

    public int Id { get; set; }

    public required string Key { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public bool IsEnabled { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
