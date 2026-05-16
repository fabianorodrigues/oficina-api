using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.Observability;
using Oficina.Application.Shared;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.UseCases.Oficina;

public class ObterOrdemServicoUseCase
{
    private readonly IOficinaRepository _repo;
    public ObterOrdemServicoUseCase(IOficinaRepository repo) => _repo = repo;

    public async Task<(OrdemServico os, Orcamento? orcamento)> Executar(Guid id, CancellationToken ct)
    {
        var os = await _repo.ObterOrdemServico(id, ct) ?? throw new OficinaException("Ordem de serviÃ§o nÃ£o encontrada.", 404);
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
    private readonly ILogger<FinalizarOrdemServicoUseCase> _logger;

    public FinalizarOrdemServicoUseCase(IOficinaRepository repo, ILogger<FinalizarOrdemServicoUseCase>? logger = null)
    {
        _repo = repo;
        _logger = logger ?? NullLogger<FinalizarOrdemServicoUseCase>.Instance;
    }

    public async Task Executar(Guid ordemServicoId, CancellationToken ct)
    {
        try
        {
            var os = await _repo.ObterOrdemServico(ordemServicoId, ct)
                     ?? throw new OficinaException("Ordem de serviÃ§o nÃ£o encontrada.", 404);

            var statusAnterior = os.Status;
            var dataStatusAnterior = os.DataUltimaAtualizacaoStatus;

            os.Finalizar();
            await _repo.Salvar(ct);

            _logger.OrdemServicoStatusAlterado(os, statusAnterior, dataStatusAnterior);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.OrdemServicoFalha(ex, ordemServicoId);
            throw;
        }
    }
}

public class EntregarOrdemServicoUseCase
{
    private readonly IOficinaRepository _repo;
    private readonly ILogger<EntregarOrdemServicoUseCase> _logger;

    public EntregarOrdemServicoUseCase(IOficinaRepository repo, ILogger<EntregarOrdemServicoUseCase>? logger = null)
    {
        _repo = repo;
        _logger = logger ?? NullLogger<EntregarOrdemServicoUseCase>.Instance;
    }

    public async Task Executar(Guid ordemServicoId, CancellationToken ct)
    {
        try
        {
            var os = await _repo.ObterOrdemServico(ordemServicoId, ct)
                     ?? throw new OficinaException("Ordem de serviÃ§o nÃ£o encontrada.", 404);

            var statusAnterior = os.Status;
            var dataStatusAnterior = os.DataUltimaAtualizacaoStatus;

            os.MarcarEntregue();
            await _repo.Salvar(ct);

            _logger.OrdemServicoStatusAlterado(os, statusAnterior, dataStatusAnterior);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.OrdemServicoFalha(ex, ordemServicoId);
            throw;
        }
    }
}
