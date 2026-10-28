using System.Text;
using OCasaGrande.Web.Auth;

namespace OCasaGrande.Web.Security;

// Generates the value for Admin:PasswordHash so the plain password never has to be stored anywhere.
public static class PasswordHashCommand
{
    public const string Name = "hash-password";

    public static int Run()
    {
        var password = ReadSecret("Senha do administrador: ");
        var confirmation = ReadSecret("Confirme a senha: ");

        if (password != confirmation)
        {
            Console.Error.WriteLine("As senhas não conferem.");
            return 1;
        }

        if (password.Length is < AdminCredentialValidator.MinPasswordLength or > AdminCredentialValidator.MaxPasswordLength)
        {
            Console.Error.WriteLine(
                $"A senha precisa ter entre {AdminCredentialValidator.MinPasswordLength} e {AdminCredentialValidator.MaxPasswordLength} caracteres.");
            return 1;
        }

        Console.WriteLine(AdminCredentialValidator.HashPassword(password));
        return 0;
    }

    private static string ReadSecret(string prompt)
    {
        Console.Write(prompt);

        if (Console.IsInputRedirected)
        {
            var line = Console.ReadLine() ?? string.Empty;
            Console.WriteLine();
            return line;
        }

        var buffer = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                buffer.Append(key.KeyChar);
            }
        }

        Console.WriteLine();
        return buffer.ToString();
    }
}
