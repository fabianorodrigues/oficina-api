using Oficina.Domain.SharedKernel;

namespace Oficina.Domain.CatalogoEstoque;

public class EstoqueInsumo : AgregadoRaiz
{
    private EstoqueInsumo() { } // EF

    public EstoqueInsumo(Guid insumoId, int quantidadeInicial)
    {
        if (insumoId == Guid.Empty) throw new ArgumentException("Insumo inválido.");
        InsumoId = insumoId;
        Quantidade = quantidadeInicial;
    }

    public Guid InsumoId { get; private set; }
    public int Quantidade { get; private set; }

    public void Ajustar(int quantidade) => Quantidade += quantidade;

    public void Baixar(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        Quantidade -= quantidade; // pode ficar negativo (compra posterior)
    }
}
