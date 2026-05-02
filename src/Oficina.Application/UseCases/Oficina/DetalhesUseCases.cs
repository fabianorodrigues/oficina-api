using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.Shared;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.UseCases.Oficina;

public class ObterOrcamentoDetalhadoUseCase
{
    private readonly IOficinaRepository _oficina;
    private readonly ICatalogoEstoqueRepository _catalogo;

    public ObterOrcamentoDetalhadoUseCase(IOficinaRepository oficina, ICatalogoEstoqueRepository catalogo)
    {
        _oficina = oficina;
        _catalogo = catalogo;
    }

    public async Task<OrcamentoDetalheResponse> Executar(Guid id, CancellationToken ct)
    {
        var orcamento = await _oficina.ObterOrcamento(id, ct)
                       ?? throw new OficinaException("Orçamento não encontrado.", 404);

        var itensMaterial = await MontarItensMaterial(orcamento, ct);

        return new OrcamentoDetalheResponse
        {
            Id = orcamento.Id,
            OrdemServicoId = orcamento.OrdemServicoId,
            Status = orcamento.Status.ToString(),
            ValorTotal = orcamento.ValorTotal,
            DataCriacao = orcamento.DataCriacao,
            ItensServico = orcamento.ItensServico.Select(x => new ItemServicoDetalheResponse
            {
                ServicoId = x.ServicoId,
                ValorMaoDeObra = x.ValorMaoDeObra
            }).ToList(),
            ItensMaterial = itensMaterial
        };
    }

    private async Task<IReadOnlyList<ItemMaterialDetalheResponse>> MontarItensMaterial(Orcamento orcamento, CancellationToken ct)
    {
        var itensMaterial = new List<ItemMaterialDetalheResponse>();
        foreach (var x in orcamento.ItensMaterial)
        {
            var descricao = x.Tipo == TipoMaterial.Peca
                ? (await _catalogo.ObterPeca(x.MaterialId, ct))?.Descricao
                : (await _catalogo.ObterInsumo(x.MaterialId, ct))?.Descricao;

            itensMaterial.Add(new ItemMaterialDetalheResponse
            {
                Tipo = x.Tipo.ToString(),
                MaterialId = x.MaterialId,
                Quantidade = x.Quantidade,
                ValorUnitario = x.ValorUnitario,
                Descricao = descricao
            });
        }

        return itensMaterial;
    }
}

public class ObterOrdemServicoDetalhadaUseCase
{
    private readonly IOficinaRepository _oficina;
    private readonly ObterOrcamentoDetalhadoUseCase _orcamentoDetalhado;

    public ObterOrdemServicoDetalhadaUseCase(IOficinaRepository oficina, ObterOrcamentoDetalhadoUseCase orcamentoDetalhado)
    {
        _oficina = oficina;
        _orcamentoDetalhado = orcamentoDetalhado;
    }

    public async Task<OrdemServicoDetalheResponse> Executar(Guid ordemServicoId, CancellationToken ct)
    {
        var os = await _oficina.ObterOrdemServico(ordemServicoId, ct)
                 ?? throw new OficinaException("Ordem de serviço não encontrada.", 404);

        OrcamentoDetalheResponse? orcamento = null;
        if (os.OrcamentoId is not null)
            orcamento = await _orcamentoDetalhado.Executar(os.OrcamentoId.Value, ct);

        return new OrdemServicoDetalheResponse
        {
            Id = os.Id,
            VeiculoId = os.VeiculoId,
            TipoManutencao = os.TipoManutencao.ToString(),
            Status = os.Status.ToString(),
            OrigemUltimaAtualizacaoStatus = os.OrigemUltimaAtualizacaoStatus.ToString(),
            DataUltimaAtualizacaoStatus = os.DataUltimaAtualizacaoStatus,
            DataCriacao = os.DataCriacao,
            DataInicioExecucao = os.DataInicioExecucao,
            DataFimExecucao = os.DataFimExecucao,
            Diagnostico = os.Diagnostico is null
                ? null
                : new DiagnosticoDetalheResponse
                {
                    Descricao = os.Diagnostico.Descricao,
                    DataRegistro = os.Diagnostico.DataRegistro
                },
            Orcamento = orcamento
        };
    }
}
