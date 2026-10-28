using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace OCasaGrande.Web.Auth;

public sealed class AdminCredentialValidator
{
    public const int MinPasswordLength = 12;
    public const int MaxPasswordLength = 128;
    public const int MaxEmailLength = 254;

    private const string SessionStampClaim = "ocg:stamp";

    private static readonly PasswordHasher<AdminAccountOptions> Hasher = new();
    private static readonly AdminAccountOptions HashSubject = new();

    private readonly string? _email;
    private readonly string? _passwordHash;
    private readonly string _sessionStamp;
    private readonly string _decoyHash = HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    public AdminCredentialValidator(IOptions<AdminAccountOptions> options)
    {
        _email = options.Value.Email?.Trim();
        _passwordHash = options.Value.PasswordHash;
        _sessionStamp = IsConfigured ? Stamp(_email!, _passwordHash!) : string.Empty;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_email) && !string.IsNullOrWhiteSpace(_passwordHash);

    public static string HashPassword(string password) => Hasher.HashPassword(HashSubject, password);

    public static bool IsValidHash(string hash) =>
        Convert.TryFromBase64String(hash, new byte[hash.Length], out var length) && length > 0;

    public bool Validate(string email, string password)
    {
        var emailMatches = IsConfigured && string.Equals(email.Trim(), _email, StringComparison.OrdinalIgnoreCase);

        // The hash is always verified, even for unknown e-mails, so response time does not reveal which e-mail is the admin's.
        var result = Hasher.VerifyHashedPassword(HashSubject, emailMatches ? _passwordHash! : _decoyHash, password);

        return emailMatches && result != PasswordVerificationResult.Failed;
    }

    public ClaimsPrincipal CreatePrincipal()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, _email!),
                new Claim(ClaimTypes.Role, AdminAuthentication.AdminRole),
                new Claim(SessionStampClaim, _sessionStamp),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }

    // Changing the admin e-mail or password in configuration invalidates every existing session.
    public bool IsCurrentSession(ClaimsPrincipal? principal) =>
        IsConfigured &&
        principal?.FindFirstValue(SessionStampClaim) is { } stamp &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(stamp), Encoding.UTF8.GetBytes(_sessionStamp));

    private static string Stamp(string email, string passwordHash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{email.ToUpperInvariant()}|{passwordHash}")));
}
