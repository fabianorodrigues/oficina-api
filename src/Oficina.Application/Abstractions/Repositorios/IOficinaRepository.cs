using Oficina.Domain.Oficina;

namespace Oficina.Application.Abstractions.Repositorios;

public interface IOficinaRepository
{
    Task<OrdemServico?> ObterOrdemServico(Guid id, CancellationToken ct);
    Task<IReadOnlyList<OrdemServico>> ListarOrdensServico(CancellationToken ct);
    Task<IReadOnlyList<OrdemServico>> ListarOrdensServicoPorVeiculos(IReadOnlyCollection<Guid> veiculoIds, CancellationToken ct);
    Task AdicionarOrdemServico(OrdemServico os, CancellationToken ct);

    Task<Orcamento?> ObterOrcamento(Guid id, CancellationToken ct);
    Task<Orcamento?> ObterOrcamentoPorTokenAcaoExterna(string token, CancellationToken ct);
    Task<Orcamento?> ObterOrcamentoPorOs(Guid ordemServicoId, CancellationToken ct);
    Task AdicionarOrcamento(Orcamento orcamento, CancellationToken ct);

    Task Salvar(CancellationToken ct);
}
