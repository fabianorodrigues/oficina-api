namespace Oficina.Domain.Cadastro.ValueObjects;

public sealed class Contato
{
    public string Email { get; private set; } = default!;
    public string Telefone { get; private set; } = default!;

    private Contato() { } // EF

    public Contato(string email, string telefone)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email é obrigatório.");

        if (string.IsNullOrWhiteSpace(telefone))
            throw new ArgumentException("Telefone é obrigatório.");

        Email = email.Trim();
        Telefone = telefone.Trim();
    }
}
