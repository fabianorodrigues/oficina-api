namespace Oficina.Domain.Cadastro.ValueObjects;

public sealed class Modelo
{
    public string Descricao { get; private set; } = default!;
    public string Marca { get; private set; } = default!;
    public int Ano { get; private set; }

    private Modelo() { } // EF

    public Modelo(string descricao, string marca, int ano)
    {
        var descricaoNorm = NormalizarTexto(descricao);
        var marcaNorm = NormalizarTexto(marca);

        if (string.IsNullOrWhiteSpace(descricaoNorm))
            throw new ArgumentException("Descricao do modelo e obrigatoria.");

        if (string.IsNullOrWhiteSpace(marcaNorm))
            throw new ArgumentException("Marca e obrigatoria.");

        if (ano < 1900 || ano > DateTime.Now.Year)
            throw new ArgumentException("Ano e obrigatorio.");

        Descricao = descricaoNorm;
        Marca = marcaNorm;
        Ano = ano;
    }

    private static string NormalizarTexto(string valor)
        => (valor ?? string.Empty).Trim();
}
