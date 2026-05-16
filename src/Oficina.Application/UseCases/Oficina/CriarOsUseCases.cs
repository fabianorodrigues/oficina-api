using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Common;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.Observability;
using Oficina.Application.Shared;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.UseCases.Oficina;

public class CriarOsPreventivaUseCase
{
    private static readonly TimeSpan PrazoExpiracaoAcaoExterna = TimeSpan.FromDays(7);
    private readonly ICadastroRepository _cadastro;
    private readonly ICatalogoEstoqueRepository _catalogo;
    private readonly IOficinaRepository _oficina;
    private readonly INotificadorCliente _notificador;
    private readonly ILogger<CriarOsPreventivaUseCase> _logger;

    public CriarOsPreventivaUseCase(
        ICadastroRepository cadastro,
        ICatalogoEstoqueRepository catalogo,
        IOficinaRepository oficina,
        INotificadorCliente notificador,
        ILogger<CriarOsPreventivaUseCase>? logger = null)
    {
        _cadastro = cadastro;
        _catalogo = catalogo;
        _oficina = oficina;
        _notificador = notificador;
        _logger = logger ?? NullLogger<CriarOsPreventivaUseCase>.Instance;
    }

    public async Task<CriarOsPreventivaResponse> Executar(Guid veiculoId, IReadOnlyList<Guid> servicoIds, CancellationToken ct)
    {
        OrdemServico? os = null;

        try
        {
            var veiculo = await _cadastro.ObterVeiculo(veiculoId, ct);
            if (veiculo is null) throw new OficinaException("VeÃ­culo nÃ£o encontrado.", 404);

            os = OrdemServico.CriarPreventiva(veiculoId, servicoIds);

            var orcamento = await GerarOrcamento(os, ct);
            orcamento.DefinirTokenAcaoExterna(
                TokenAcaoExternaGenerator.Gerar(),
                DateTimeOffset.UtcNow.Add(PrazoExpiracaoAcaoExterna));
            os.VincularOrcamento(orcamento.Id);

            await _oficina.AdicionarOrdemServico(os, ct);
            await _oficina.AdicionarOrcamento(orcamento, ct);
            await _oficina.Salvar(ct);

            _logger.OrdemServicoCriada(os);
            _logger.OrdemServicoStatusAlterado(os, StatusOrdemServico.Recebida, default);

            await _notificador.NotificarOrcamentoCriado(orcamento.Id, os.Id, ct);

            return new CriarOsPreventivaResponse
            {
                Id = os.Id,
                OrcamentoId = orcamento.Id
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.OrdemServicoFalha(ex, os?.Id);
            throw;
        }
    }

    private async Task<Orcamento> GerarOrcamento(OrdemServico os, CancellationToken ct)
    {
        var itensServico = new List<OrcamentoItemServico>();
        var itensMaterial = new List<OrcamentoItemMaterial>();
        decimal total = 0m;

        foreach (var item in os.ItensServico)
        {
            var servico = await _catalogo.ObterServico(item.ServicoId, ct)
                         ?? throw new OficinaException($"ServiÃ§o nÃ£o encontrado: {item.ServicoId}", 404);

            itensServico.Add(new OrcamentoItemServico(servico.Id, servico.MaoDeObra));
            total += servico.MaoDeObra;

            foreach (var p in servico.Pecas)
            {
                var peca = await _catalogo.ObterPeca(p.PecaId, ct)
                           ?? throw new OficinaException($"PeÃ§a nÃ£o encontrada: {p.PecaId}", 404);

                itensMaterial.Add(new OrcamentoItemMaterial(TipoMaterial.Peca, peca.Id, p.Quantidade, peca.PrecoUnitario));
                total += p.Quantidade * peca.PrecoUnitario;
            }

            foreach (var ins in servico.Insumos)
            {
                var insumo = await _catalogo.ObterInsumo(ins.InsumoId, ct)
                            ?? throw new OficinaException($"Insumo nÃ£o encontrado: {ins.InsumoId}", 404);

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
    private readonly ILogger<CriarOsCorretivaUseCase> _logger;

    public CriarOsCorretivaUseCase(
        ICadastroRepository cadastro,
        IOficinaRepository oficina,
        ILogger<CriarOsCorretivaUseCase>? logger = null)
    {
        _cadastro = cadastro;
        _oficina = oficina;
        _logger = logger ?? NullLogger<CriarOsCorretivaUseCase>.Instance;
    }

    public async Task<CriarOsCorretivaResponse> Executar(Guid veiculoId, CancellationToken ct)
    {
        OrdemServico? os = null;

        try
        {
            var veiculo = await _cadastro.ObterVeiculo(veiculoId, ct);
            if (veiculo is null) throw new OficinaException("VeÃ­culo nÃ£o encontrado.", 404);

            os = OrdemServico.CriarCorretiva(veiculoId);

            await _oficina.AdicionarOrdemServico(os, ct);
            await _oficina.Salvar(ct);

            _logger.OrdemServicoCriada(os);
            _logger.OrdemServicoStatusAlterado(os, StatusOrdemServico.Recebida, default);

            return new CriarOsCorretivaResponse
            {
                Id = os.Id
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.OrdemServicoFalha(ex, os?.Id);
            throw;
        }
    }
}
