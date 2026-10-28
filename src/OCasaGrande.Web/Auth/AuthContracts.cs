namespace OCasaGrande.Web.Auth;

public sealed record LoginRequest(string? Email, string? Password)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(Email) || Email.Length > AdminCredentialValidator.MaxEmailLength)
        {
            errors[nameof(Email)] = ["Informe um e-mail válido."];
        }

        if (string.IsNullOrEmpty(Password) || Password.Length > AdminCredentialValidator.MaxPasswordLength)
        {
            errors[nameof(Password)] = ["Informe a senha."];
        }

        return errors;
    }
}

public sealed record CsrfTokenResponse(string Token);

public sealed record SessionResponse(string Email);
