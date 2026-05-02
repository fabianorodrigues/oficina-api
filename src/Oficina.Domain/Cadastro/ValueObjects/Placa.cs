using System.Text.RegularExpressions;

namespace Oficina.Domain.Cadastro.ValueObjects;

public sealed class Placa
{
    public string Valor { get; private set; } = default!;

    private Placa() { } // EF

    public Placa(string valor)
    {
        Valor = Normalizar(valor);
        Validar(Valor);
    }

    private static string Normalizar(string valor)
        => (valor ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Replace("-", "");

    private static void Validar(string valorNormalizado)
    {
        if (string.IsNullOrWhiteSpace(valorNormalizado))
            throw new ArgumentException("Placa é obrigatória.");

        if (valorNormalizado.Length != 7)
            throw new ArgumentException("Placa deve ter 7 caracteres.");

        if (!valorNormalizado.Any(char.IsLetter) || !valorNormalizado.Any(char.IsNumber))
            throw new ArgumentException("Placa deve conter apenas letras e números.");
    }
}
