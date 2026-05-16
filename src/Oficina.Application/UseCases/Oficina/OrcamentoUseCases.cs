using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Observability;
using Oficina.Application.Shared;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.UseCases.Oficina;

public class AprovarOrcamentoUseCase
{
    private readonly IOficinaRepository _oficina;
    private readonly ICatalogoEstoqueRepository _estoqueRepo;
    private readonly ILogger<AprovarOrcamentoUseCase> _logger;

    public AprovarOrcamentoUseCase(
        IOficinaRepository oficina,
        ICatalogoEstoqueRepository estoqueRepo,
        ILogger<AprovarOrcamentoUseCase>? logger = null)
    {
        _oficina = oficina;
        _estoqueRepo = estoqueRepo;
        _logger = logger ?? NullLogger<AprovarOrcamentoUseCase>.Instance;
    }

    public async Task Executar(
        Guid orcamentoId,
        CancellationToken ct,
        OrigemAtualizacaoStatusOs origemAtualizacaoStatus = OrigemAtualizacaoStatusOs.Interna)
    {
        Guid? ordemServicoId = null;

        try
        {
            var orcamento = await _oficina.ObterOrcamento(orcamentoId, ct)
                           ?? throw new OficinaException("OrÃ§amento nÃ£o encontrado.", 404);

            var os = await _oficina.ObterOrdemServico(orcamento.OrdemServicoId, ct)
                     ?? throw new OficinaException("Ordem de serviÃ§o nÃ£o encontrada.", 404);

            ordemServicoId = os.Id;
            var statusAnterior = os.Status;
            var dataStatusAnterior = os.DataUltimaAtualizacaoStatus;

            orcamento.Aprovar();

            // baixa estoque apÃ³s aprovaÃ§Ã£o
            foreach (var m in orcamento.ItensMaterial)
            {
                if (m.Tipo == TipoMaterial.Peca)
                {
                    var estoque = await _estoqueRepo.ObterEstoquePeca(m.MaterialId, ct)
                                  ?? throw new OficinaException("Estoque da peÃ§a nÃ£o encontrado.", 404);

                    estoque.Baixar(m.Quantidade);
                }
                else
                {
                    var estoque = await _estoqueRepo.ObterEstoqueInsumo(m.MaterialId, ct)
                                  ?? throw new OficinaException("Estoque do insumo nÃ£o encontrado.", 404);

                    estoque.Baixar(m.Quantidade);
                }
            }

            os.IniciarExecucao(orcamento, origemAtualizacaoStatus);

            await _estoqueRepo.Salvar(ct);
            await _oficina.Salvar(ct);

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

public class RecusarOrcamentoUseCase
{
    private readonly IOficinaRepository _oficina;
    private readonly INotificadorCliente _notificador;
    private readonly ILogger<RecusarOrcamentoUseCase> _logger;

    public RecusarOrcamentoUseCase(
        IOficinaRepository oficina,
        INotificadorCliente notificador,
        ILogger<RecusarOrcamentoUseCase>? logger = null)
    {
        _oficina = oficina;
        _notificador = notificador;
        _logger = logger ?? NullLogger<RecusarOrcamentoUseCase>.Instance;
    }

    public async Task Executar(
        Guid orcamentoId,
        CancellationToken ct,
        OrigemAtualizacaoStatusOs origemAtualizacaoStatus = OrigemAtualizacaoStatusOs.Interna)
    {
        Guid? ordemServicoId = null;

        try
        {
            var orcamento = await _oficina.ObterOrcamento(orcamentoId, ct)
                           ?? throw new OficinaException("OrÃ§amento nÃ£o encontrado.", 404);

            var os = await _oficina.ObterOrdemServico(orcamento.OrdemServicoId, ct)
                     ?? throw new OficinaException("Ordem de serviÃ§o nÃ£o encontrada.", 404);

            ordemServicoId = os.Id;
            var statusAnterior = os.Status;
            var dataStatusAnterior = os.DataUltimaAtualizacaoStatus;

            orcamento.Recusar();
            os.FinalizarPorRecusaOrcamento(orcamento, origemAtualizacaoStatus);

            await _oficina.Salvar(ct);

            _logger.OrdemServicoStatusAlterado(os, statusAnterior, dataStatusAnterior);

            await _notificador.NotificarOrcamentoRecusado(orcamento.Id, os.Id, ct);
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

public class ObterOrcamentoUseCase
{
    private readonly IOficinaRepository _repo;
    public ObterOrcamentoUseCase(IOficinaRepository repo) => _repo = repo;

    public async Task<Orcamento> Executar(Guid id, CancellationToken ct)
        => await _repo.ObterOrcamento(id, ct) ?? throw new OficinaException("OrÃ§amento nÃ£o encontrado.", 404);
}
