using Oficina.Application.Shared;
using Oficina.Application.Common;
using Oficina.Application.DTO.Oficina;
using Oficina.Domain.Oficina.Enums;
using Oficina.Domain.Oficina;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;

namespace Oficina.Application.UseCases.Oficina;

public class CriarOsPreventivaUseCase
{
    private static readonly TimeSpan PrazoExpiracaoAcaoExterna = TimeSpan.FromDays(7);
    private readonly ICadastroRepository _cadastro;
    private readonly ICatalogoEstoqueRepository _catalogo;
    private readonly IOficinaRepository _oficina;
    private readonly INotificadorCliente _notificador;

    public CriarOsPreventivaUseCase(
        ICadastroRepository cadastro,
        ICatalogoEstoqueRepository catalogo,
        IOficinaRepository oficina,
        INotificadorCliente notificador)
    {
        _cadastro = cadastro;
        _catalogo = catalogo;
        _oficina = oficina;
        _notificador = notificador;
    }

    public async Task<CriarOsPreventivaResponse> Executar(Guid veiculoId, IReadOnlyList<Guid> servicoIds, CancellationToken ct)
    {
        var veiculo = await _cadastro.ObterVeiculo(veiculoId, ct);
        if (veiculo is null) throw new OficinaException("Veículo não encontrado.", 404);

        var os = OrdemServico.CriarPreventiva(veiculoId, servicoIds);

        var orcamento = await GerarOrcamento(os, ct);
        orcamento.DefinirTokenAcaoExterna(
            TokenAcaoExternaGenerator.Gerar(),
            DateTimeOffset.UtcNow.Add(PrazoExpiracaoAcaoExterna));
        os.VincularOrcamento(orcamento.Id);

        await _oficina.AdicionarOrdemServico(os, ct);
        await _oficina.AdicionarOrcamento(orcamento, ct);
        await _oficina.Salvar(ct);

        await _notificador.NotificarOrcamentoCriado(orcamento.Id, os.Id, ct);

        return new CriarOsPreventivaResponse
        {
            Id = os.Id,
            OrcamentoId = orcamento.Id
        };
    }

    private async Task<Orcamento> GerarOrcamento(OrdemServico os, CancellationToken ct)
    {
        var itensServico = new List<OrcamentoItemServico>();
        var itensMaterial = new List<OrcamentoItemMaterial>();
        decimal total = 0m;

        foreach (var item in os.ItensServico)
        {
            var servico = await _catalogo.ObterServico(item.ServicoId, ct)
                         ?? throw new OficinaException($"Serviço não encontrado: {item.ServicoId}", 404);

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

        var orcamento = new Orcamento(os.Id, total);
        orcamento.DefinirItensServico(itensServico);
        orcamento.DefinirItensMaterial(itensMaterial);
        return orcamento;
    }
}

public class CriarOsCorretivaUseCase
{
    private readonly ICadastroRepository _cadastro;
    private readonly IOficinaRepository _oficina;

    public CriarOsCorretivaUseCase(ICadastroRepository cadastro, IOficinaRepository oficina)
    {
        _cadastro = cadastro;
        _oficina = oficina;
    }

    public async Task<CriarOsCorretivaResponse> Executar(Guid veiculoId, CancellationToken ct)
    {
        var veiculo = await _cadastro.ObterVeiculo(veiculoId, ct);
        if (veiculo is null) throw new OficinaException("Veículo não encontrado.", 404);

        var os = OrdemServico.CriarCorretiva(veiculoId);

        await _oficina.AdicionarOrdemServico(os, ct);
        await _oficina.Salvar(ct);

        return new CriarOsCorretivaResponse
        {
            Id = os.Id
        };
    }
}
