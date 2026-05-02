using Oficina.Application.Shared;
using Oficina.Application.Common;
using Oficina.Application.DTO.Oficina;
using Oficina.Domain.Oficina.Enums;
using Oficina.Domain.Oficina;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;

namespace Oficina.Application.UseCases.Oficina;

public class RegistrarDiagnosticoUseCase
{
    private static readonly TimeSpan PrazoExpiracaoAcaoExterna = TimeSpan.FromDays(7);
    private readonly IOficinaRepository _oficina;
    private readonly ICatalogoEstoqueRepository _catalogo;
    private readonly INotificadorCliente _notificador;

    public RegistrarDiagnosticoUseCase(
        IOficinaRepository oficina,
        ICatalogoEstoqueRepository catalogo,
        INotificadorCliente notificador)
    {
        _oficina = oficina;
        _catalogo = catalogo;
        _notificador = notificador;
    }

    public async Task<RegistrarDiagnosticoResponse> Executar(Guid ordemServicoId, string descricao, IReadOnlyList<Guid> servicoIds, CancellationToken ct)
    {
        var os = await _oficina.ObterOrdemServico(ordemServicoId, ct)
                 ?? throw new OficinaException("Ordem de serviço não encontrada.", 404);

        os.RegistrarDiagnostico(descricao, servicoIds);
        await _oficina.Salvar(ct);

        var orcamento = await GerarOrcamento(os.Id, servicoIds, ct);
        orcamento.DefinirTokenAcaoExterna(
            TokenAcaoExternaGenerator.Gerar(),
            DateTimeOffset.UtcNow.Add(PrazoExpiracaoAcaoExterna));
        os.VincularOrcamento(orcamento.Id);

        await _oficina.AdicionarOrcamento(orcamento, ct);
        await _oficina.Salvar(ct);

        await _notificador.NotificarOrcamentoCriado(orcamento.Id, os.Id, ct);

        return new RegistrarDiagnosticoResponse
        {
            OrcamentoId = orcamento.Id
        };
    }

    private async Task<Orcamento> GerarOrcamento(Guid osId, IEnumerable<Guid> servicoIds, CancellationToken ct)
    {
        var itensServico = new List<OrcamentoItemServico>();
        var itensMaterial = new List<OrcamentoItemMaterial>();
        decimal total = 0m;

        foreach (var servicoId in servicoIds)
        {
            var servico = await _catalogo.ObterServico(servicoId, ct)
                         ?? throw new OficinaException($"Serviço não encontrado: {servicoId}", 404);

            itensServico.Add(new OrcamentoItemServico(servico.Id, servico.MaoDeObra));
            total += servico.MaoDeObra;

            foreach (var p in servico.Pecas)
            {
                var peca = await _catalogo.ObterPeca(p.PecaId, ct)
                           ?? throw new OficinaException($"Peça não encontrada: {p.PecaId}", 404);

                itensMaterial.Add(new OrcamentoItemMaterial(TipoMaterial.Peca, peca.Id, p.Quantidade, peca.PrecoUnitario));
                total += p.Quantidade * peca.PrecoUnitario;
            }

            foreach (var ins in servico.Insumos)
            {
                var insumo = await _catalogo.ObterInsumo(ins.InsumoId, ct)
                            ?? throw new OficinaException($"Insumo não encontrado: {ins.InsumoId}", 404);

                itensMaterial.Add(new OrcamentoItemMaterial(TipoMaterial.Insumo, insumo.Id, ins.Quantidade, insumo.PrecoUnitario));
                total += ins.Quantidade * insumo.PrecoUnitario;
            }
        }

        var orcamento = new Orcamento(osId, total);
        orcamento.DefinirItensServico(itensServico);
        orcamento.DefinirItensMaterial(itensMaterial);
        return orcamento;
    }
}
