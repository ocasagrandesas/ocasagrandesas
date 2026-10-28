namespace OCasaGrande.Web.Auth;

// Bound from the "Admin" section. Values come from user-secrets locally and from Vercel environment variables in production.
public sealed class AdminAccountOptions
{
    public const string SectionName = "Admin";

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }
}
