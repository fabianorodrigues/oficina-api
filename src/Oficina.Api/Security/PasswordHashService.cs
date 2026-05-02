using Microsoft.AspNetCore.Identity;
using Oficina.Application.Abstractions.Seguranca;

namespace Oficina.Api.Security;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException("Senha e obrigatoria.", nameof(senha));

        return _hasher.HashPassword(new object(), senha);
    }

    public bool Verificar(string senhaHash, string senha)
    {
        if (string.IsNullOrWhiteSpace(senhaHash) || string.IsNullOrWhiteSpace(senha))
            return false;

        var resultado = _hasher.VerifyHashedPassword(new object(), senhaHash, senha);
        return resultado is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
