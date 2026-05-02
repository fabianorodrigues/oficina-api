using Oficina.Domain.SharedKernel;

namespace Oficina.Domain.CatalogoEstoque;

public class Insumo : AgregadoRaiz
{
    private Insumo() { } // EF

    public Insumo(decimal precoUnitario, string descricao)
    {
        DefinirDescricao(descricao);
        DefinirPreco(precoUnitario);
    }

    public string Descricao { get; private set; } = default!;
    public decimal PrecoUnitario { get; private set; }

    public void DefinirDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descricao e obrigatoria.");

        Descricao = descricao.Trim();
    }

    public void DefinirPreco(decimal precoUnitario)
    {
        if (precoUnitario < 0) throw new ArgumentOutOfRangeException(nameof(precoUnitario));
        PrecoUnitario = precoUnitario;
    }
}
