using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.UseCases.Oficina;

public class AprovarOrcamentoUseCase
{
    private readonly IOficinaRepository _oficina;
    private readonly ICatalogoEstoqueRepository _estoqueRepo;

    public AprovarOrcamentoUseCase(IOficinaRepository oficina, ICatalogoEstoqueRepository estoqueRepo)
    {
        _oficina = oficina;
        _estoqueRepo = estoqueRepo;
    }

    public async Task Executar(
        Guid orcamentoId,
        CancellationToken ct,
        OrigemAtualizacaoStatusOs origemAtualizacaoStatus = OrigemAtualizacaoStatusOs.Interna)
    {
        var orcamento = await _oficina.ObterOrcamento(orcamentoId, ct)
                       ?? throw new OficinaException("Orçamento não encontrado.", 404);

        var os = await _oficina.ObterOrdemServico(orcamento.OrdemServicoId, ct)
                 ?? throw new OficinaException("Ordem de serviço não encontrada.", 404);

        orcamento.Aprovar();

        // baixa estoque após aprovação
        foreach (var m in orcamento.ItensMaterial)
        {
            if (m.Tipo == TipoMaterial.Peca)
            {
                var estoque = await _estoqueRepo.ObterEstoquePeca(m.MaterialId, ct)
                              ?? throw new OficinaException("Estoque da peça não encontrado.", 404);

                estoque.Baixar(m.Quantidade);
            }
            else
            {
                var estoque = await _estoqueRepo.ObterEstoqueInsumo(m.MaterialId, ct)
                              ?? throw new OficinaException("Estoque do insumo não encontrado.", 404);

                estoque.Baixar(m.Quantidade);
            }
        }

        os.IniciarExecucao(orcamento, origemAtualizacaoStatus);

        await _estoqueRepo.Salvar(ct);
        await _oficina.Salvar(ct);
    }
}

public class RecusarOrcamentoUseCase
{
    private readonly IOficinaRepository _oficina;
    private readonly INotificadorCliente _notificador;

    public RecusarOrcamentoUseCase(IOficinaRepository oficina, INotificadorCliente notificador)
    {
        _oficina = oficina;
        _notificador = notificador;
    }

    public async Task Executar(
        Guid orcamentoId,
        CancellationToken ct,
        OrigemAtualizacaoStatusOs origemAtualizacaoStatus = OrigemAtualizacaoStatusOs.Interna)
    {
        var orcamento = await _oficina.ObterOrcamento(orcamentoId, ct)
                       ?? throw new OficinaException("Orçamento não encontrado.", 404);

        var os = await _oficina.ObterOrdemServico(orcamento.OrdemServicoId, ct)
                 ?? throw new OficinaException("Ordem de serviço não encontrada.", 404);

        orcamento.Recusar();
        os.FinalizarPorRecusaOrcamento(orcamento, origemAtualizacaoStatus);

        await _oficina.Salvar(ct);

        await _notificador.NotificarOrcamentoRecusado(orcamento.Id, os.Id, ct);
    }
}

public class ObterOrcamentoUseCase
{
    private readonly IOficinaRepository _repo;
    public ObterOrcamentoUseCase(IOficinaRepository repo) => _repo = repo;

    public async Task<Orcamento> Executar(Guid id, CancellationToken ct)
        => await _repo.ObterOrcamento(id, ct) ?? throw new OficinaException("Orçamento não encontrado.", 404);
}
