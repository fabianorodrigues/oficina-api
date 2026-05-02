using Oficina.Domain.SharedKernel;

namespace Oficina.Domain.CatalogoEstoque;

public class Servico : AgregadoRaiz
{
    private readonly List<ServicoPecaRequerida> _pecas = [];
    private readonly List<ServicoInsumoRequerido> _insumos = [];

    private Servico() { } // EF

    public Servico(decimal maoDeObra)
    {
        if (maoDeObra < 0)
            throw new ArgumentOutOfRangeException(nameof(maoDeObra), "Mão de obra não pode ser negativa.");

        MaoDeObra = maoDeObra;
    }

    public decimal MaoDeObra { get; private set; }

    // Importante: expor o backing list para o EF mapear como Owned collection
    public IReadOnlyCollection<ServicoPecaRequerida> Pecas => _pecas;
    public IReadOnlyCollection<ServicoInsumoRequerido> Insumos => _insumos;

    public void DefinirMaoDeObra(decimal maoDeObra)
    {
        if (maoDeObra < 0) throw new ArgumentOutOfRangeException(nameof(maoDeObra));
        MaoDeObra = maoDeObra;
    }

    public void SubstituirPecas(IEnumerable<(Guid pecaId, int quantidade)> pecas)
    {
        _pecas.Clear();

        foreach (var (pecaId, quantidade) in pecas)
            AdicionarPeca(pecaId, quantidade);
    }

    public void SubstituirInsumos(IEnumerable<(Guid insumoId, int quantidade)> insumos)
    {
        _insumos.Clear();

        foreach (var (insumoId, quantidade) in insumos)
            AdicionarInsumo(insumoId, quantidade);
    }

    public void AdicionarPeca(Guid pecaId, int quantidade)
    {
        if (pecaId == Guid.Empty) throw new ArgumentException("Peça inválida.");
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        _pecas.Add(new ServicoPecaRequerida(pecaId, quantidade));
    }

    public void AdicionarInsumo(Guid insumoId, int quantidade)
    {
        if (insumoId == Guid.Empty) throw new ArgumentException("Insumo inválido.");
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        _insumos.Add(new ServicoInsumoRequerido(insumoId, quantidade));
    }
}

public class ServicoPecaRequerida
{
    private ServicoPecaRequerida() { } // EF
    public ServicoPecaRequerida(Guid pecaId, int quantidade)
    {
        PecaId = pecaId;
        Quantidade = quantidade;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PecaId { get; private set; }
    public int Quantidade { get; private set; }
}

public class ServicoInsumoRequerido
{
    private ServicoInsumoRequerido() { } // EF
    public ServicoInsumoRequerido(Guid insumoId, int quantidade)
    {
        InsumoId = insumoId;
        Quantidade = quantidade;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid InsumoId { get; private set; }
    public int Quantidade { get; private set; }
}
