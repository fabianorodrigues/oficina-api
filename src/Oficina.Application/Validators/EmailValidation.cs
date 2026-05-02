using System.Net.Mail;

namespace Oficina.Application.Validators;

internal static class EmailValidation
{
    public static bool EnderecoValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var valor = email.Trim();
        if (!MailAddress.TryCreate(valor, out var endereco))
            return false;

        if (!string.Equals(endereco.Address, valor, StringComparison.OrdinalIgnoreCase))
            return false;

        var at = valor.IndexOf('@');
        if (at <= 0)
            return false;

        var localPart = valor[..at];
        return !localPart.StartsWith('.') &&
               !localPart.EndsWith('.') &&
               !localPart.Contains("..");
    }
}
