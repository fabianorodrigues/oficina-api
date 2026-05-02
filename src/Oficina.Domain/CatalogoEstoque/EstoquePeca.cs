using Oficina.Domain.SharedKernel;

namespace Oficina.Domain.CatalogoEstoque;

public class EstoquePeca : AgregadoRaiz
{
    private EstoquePeca() { } // EF

    public EstoquePeca(Guid pecaId, int quantidadeInicial)
    {
        if (pecaId == Guid.Empty) throw new ArgumentException("Peça inválida.");
        PecaId = pecaId;
        Quantidade = quantidadeInicial;
    }

    public Guid PecaId { get; private set; }
    public int Quantidade { get; private set; }

    public void Ajustar(int quantidade) => Quantidade += quantidade;

    public void Baixar(int quantidade)
    {
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        Quantidade -= quantidade; // pode ficar negativo (compra posterior)
    }
}
