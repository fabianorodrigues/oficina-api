using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Observability;
using Oficina.Application.Shared;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.UseCases.Oficina;

public class ClassificarOrdemServicoUseCase
{
    private readonly IOficinaRepository _repo;
    private readonly INotificadorCliente _notificador;
    private readonly ILogger<ClassificarOrdemServicoUseCase> _logger;

    public ClassificarOrdemServicoUseCase(
        IOficinaRepository repo,
        INotificadorCliente notificador,
        ILogger<ClassificarOrdemServicoUseCase>? logger = null)
    {
        _repo = repo;
        _notificador = notificador;
        _logger = logger ?? NullLogger<ClassificarOrdemServicoUseCase>.Instance;
    }

    public async Task Executar(Guid ordemServicoId, string tipoManutencao, CancellationToken ct)
    {
        try
        {
            var os = await _repo.ObterOrdemServico(ordemServicoId, ct)
                     ?? throw new OficinaException("Ordem de serviÃ§o nÃ£o encontrada.", 404);

            if (!Enum.TryParse<TipoManutencao>(tipoManutencao, true, out var tipo) || tipo == TipoManutencao.NaoClassificada)
                throw new OficinaException("Tipo de manutenÃ§Ã£o invÃ¡lido.", 400);

            var statusAnterior = os.Status;
            var dataStatusAnterior = os.DataUltimaAtualizacaoStatus;

            os.Classificar(tipo);
            await _repo.Salvar(ct);

            _logger.OrdemServicoStatusAlterado(os, statusAnterior, dataStatusAnterior);

            if (os.TipoManutencao == TipoManutencao.Preventiva && os.OrcamentoId.HasValue)
                await _notificador.NotificarOrcamentoCriado(os.OrcamentoId.Value, os.Id, ct);
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
