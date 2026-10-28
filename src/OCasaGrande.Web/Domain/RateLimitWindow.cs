namespace OCasaGrande.Web.Domain;

// Shared fixed-window counter, so a limit holds across every running instance of the app.
public sealed class RateLimitWindow
{
    public const int KeyMaxLength = 100;

    public required string Key { get; set; }

    public DateTimeOffset WindowStart { get; set; }

    public int Count { get; set; }
}
