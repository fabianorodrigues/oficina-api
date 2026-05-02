using Oficina.Domain.CatalogoEstoque;

namespace Oficina.Application.Abstractions.Repositorios;

public interface ICatalogoEstoqueRepository
{
    Task<IReadOnlyList<Servico>> ListarServicos(CancellationToken ct);
    Task<Servico?> ObterServico(Guid id, CancellationToken ct);
    Task AdicionarServico(Servico servico, CancellationToken ct);

    Task<IReadOnlyList<Peca>> ListarPecas(CancellationToken ct);
    Task<Peca?> ObterPeca(Guid id, CancellationToken ct);
    Task AdicionarPeca(Peca peca, CancellationToken ct);

    Task<IReadOnlyList<Insumo>> ListarInsumos(CancellationToken ct);
    Task<Insumo?> ObterInsumo(Guid id, CancellationToken ct);
    Task AdicionarInsumo(Insumo insumo, CancellationToken ct);

    Task<IReadOnlyList<EstoquePeca>> ListarEstoquePecas(CancellationToken ct);
    Task<EstoquePeca?> ObterEstoquePeca(Guid pecaId, CancellationToken ct);
    Task<IReadOnlyList<EstoqueInsumo>> ListarEstoqueInsumos(CancellationToken ct);
    Task<EstoqueInsumo?> ObterEstoqueInsumo(Guid insumoId, CancellationToken ct);
    Task AdicionarEstoquePeca(EstoquePeca estoque, CancellationToken ct);
    Task AdicionarEstoqueInsumo(EstoqueInsumo estoque, CancellationToken ct);

    Task Salvar(CancellationToken ct);
}
