namespace Oficina.Domain.Cadastro.ValueObjects;

public sealed class Renavam
{
    public string Valor { get; private set; } = default!;

    private Renavam() { } // EF

    public Renavam(string valor)
    {
        Valor = Normalizar(valor);
        Validar(Valor);
    }

    private static string Normalizar(string valor)
        => new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    private static void Validar(string valorNormalizado)
    {
        if (string.IsNullOrWhiteSpace(valorNormalizado))
            throw new ArgumentException("RENAVAM é obrigatório.");

        if (valorNormalizado.Length != 11)
            throw new ArgumentException("RENAVAM deve ter 11 dígitos.");
    }
}
