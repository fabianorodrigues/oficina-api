using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Application.DTO.Oficina;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.UseCases.Oficina;

public class ObterOrdemServicoUseCase
{
    private readonly IOficinaRepository _repo;
    public ObterOrdemServicoUseCase(IOficinaRepository repo) => _repo = repo;

    public async Task<(OrdemServico os, Orcamento? orcamento)> Executar(Guid id, CancellationToken ct)
    {
        var os = await _repo.ObterOrdemServico(id, ct) ?? throw new OficinaException("Ordem de serviço não encontrada.", 404);
        var orc = os.OrcamentoId is null ? null : await _repo.ObterOrcamento(os.OrcamentoId.Value, ct);
        return (os, orc);
    }
}

public class ListarOrdensServicoUseCase
{
    private readonly IOficinaRepository _repo;
    public ListarOrdensServicoUseCase(IOficinaRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<OrdemServicoListaItemResponse>> Executar(CancellationToken ct)
    {
        var ordens = await _repo.ListarOrdensServico(ct);

        return ordens
            .Where(DeveSerListada)
            .OrderBy(os => ObterPrioridadeStatus(os.Status))
            .ThenBy(os => os.DataCriacao)
            .ThenBy(os => os.Id)
            .Select(os => new OrdemServicoListaItemResponse
            {
                Id = os.Id,
                VeiculoId = os.VeiculoId,
                TipoManutencao = os.TipoManutencao.ToString(),
                Status = os.Status.ToString(),
                DataCriacao = os.DataCriacao
            })
            .ToList();
    }

    private static bool DeveSerListada(OrdemServico os)
        => os.Status is not StatusOrdemServico.Finalizada and not StatusOrdemServico.Entregue;

    private static int ObterPrioridadeStatus(StatusOrdemServico status)
        => status switch
        {
            StatusOrdemServico.EmExecucao => 1,
            StatusOrdemServico.AguardandoAprovacao => 2,
            StatusOrdemServico.EmDiagnostico => 3,
            StatusOrdemServico.Recebida => 4,
            _ => int.MaxValue
        };
}

public class FinalizarOrdemServicoUseCase
{
    private readonly IOficinaRepository _repo;
    public FinalizarOrdemServicoUseCase(IOficinaRepository repo) => _repo = repo;

    public async Task Executar(Guid ordemServicoId, CancellationToken ct)
    {
        var os = await _repo.ObterOrdemServico(ordemServicoId, ct)
                 ?? throw new OficinaException("Ordem de serviço não encontrada.", 404);

        os.Finalizar();
        await _repo.Salvar(ct);
    }
}

public class EntregarOrdemServicoUseCase
{
    private readonly IOficinaRepository _repo;
    public EntregarOrdemServicoUseCase(IOficinaRepository repo) => _repo = repo;

    public async Task Executar(Guid ordemServicoId, CancellationToken ct)
    {
        var os = await _repo.ObterOrdemServico(ordemServicoId, ct)
                 ?? throw new OficinaException("Ordem de serviço não encontrada.", 404);

        os.MarcarEntregue();
        await _repo.Salvar(ct);
    }
}
