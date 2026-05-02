using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Common;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.Shared;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Domain.Oficina;

namespace Oficina.Application.UseCases.Oficina;

public class AbrirOrdemServicoUseCase
{
    private static readonly TimeSpan PrazoExpiracaoAcaoExterna = TimeSpan.FromDays(7);
    private readonly ICadastroRepository _cadastro;
    private readonly ICatalogoEstoqueRepository _catalogo;
    private readonly IOficinaRepository _oficina;

    public AbrirOrdemServicoUseCase(
        ICadastroRepository cadastro,
        ICatalogoEstoqueRepository catalogo,
        IOficinaRepository oficina)
    {
        _cadastro = cadastro;
        _catalogo = catalogo;
        _oficina = oficina;
    }

    public async Task<AbrirOrdemServicoResponse> Executar(AbrirOrdemServicoRequest req, CancellationToken ct)
    {
        var cliente = await ObterOuCriarCliente(req.Cliente, ct);
        var veiculo = await ObterOuCriarVeiculo(cliente.Id, req.Veiculo, ct);

        var servicoIds = req.Itens.Servicos.Select(x => x.ServicoId).Where(x => x != Guid.Empty).Distinct().ToList();
        if (servicoIds.Count == 0)
            throw new OficinaException("A abertura completa exige ao menos 1 serviço.", 400);

        var os = OrdemServico.CriarRecebida(veiculo.Id);

        var (total, itensServico, itensMaterial) = await CalcularTotal(req, ct);

        var orcamento = new Orcamento(os.Id, total);
        orcamento.DefinirTokenAcaoExterna(
           TokenAcaoExternaGenerator.Gerar(),
           DateTimeOffset.UtcNow.Add(PrazoExpiracaoAcaoExterna));
        orcamento.DefinirItensServico(itensServico);
        orcamento.DefinirItensMaterial(itensMaterial);

        os.VincularOrcamento(orcamento.Id, atualizarStatusParaAguardando: false);

        await _oficina.AdicionarOrdemServico(os, ct);
        await _oficina.AdicionarOrcamento(orcamento, ct);
        await _oficina.Salvar(ct);

        return new AbrirOrdemServicoResponse
        {
            Id = os.Id,
            Status = os.Status.ToString(),
            Total = total
        };
    }

    private async Task<Cliente> ObterOuCriarCliente(ClienteAberturaRequest req, CancellationToken ct)
    {
        var documento = new DocumentoCpfCnpj(req.Documento);
        var contato = new Contato(req.Email, req.Telefone);

        var existente = await _cadastro.ObterClientePorDocumento(documento.Valor, ct);
        if (existente is not null)
            return existente;

        var novo = new Cliente(documento, req.Nome, contato);
        await _cadastro.AdicionarCliente(novo, ct);
        await _cadastro.Salvar(ct);
        return novo;
    }

    private async Task<Veiculo> ObterOuCriarVeiculo(Guid clienteId, VeiculoAberturaRequest req, CancellationToken ct)
    {
        var placa = new Placa(req.Placa);
        var existente = await _cadastro.ObterVeiculoPorPlaca(placa.Valor, ct);
        if (existente is not null)
            return existente;

        var novo = new Veiculo(
            clienteId,
            placa,
            new Renavam(req.Renavam),
            new Modelo(req.Modelo.Descricao, req.Modelo.Marca, req.Modelo.Ano));

        await _cadastro.AdicionarVeiculo(novo, ct);
        await _cadastro.Salvar(ct);
        return novo;
    }

    private async Task<(decimal total, List<OrcamentoItemServico> itensServico, List<OrcamentoItemMaterial> itensMaterial)> CalcularTotal(
        AbrirOrdemServicoRequest req,
        CancellationToken ct)
    {
        decimal total = 0;
        var itensServico = new List<OrcamentoItemServico>();
        var itensMaterial = new List<OrcamentoItemMaterial>();

        foreach (var item in req.Itens.Servicos)
        {
            var servico = await _catalogo.ObterServico(item.ServicoId, ct)
                         ?? throw new OficinaException($"Serviço não encontrado: {item.ServicoId}", 404);

            itensServico.Add(new OrcamentoItemServico(servico.Id, servico.MaoDeObra));
            total += servico.MaoDeObra;
        }

        foreach (var item in req.Itens.Pecas)
        {
            if (item.Quantidade <= 0)
                throw new OficinaException("Quantidade de peça deve ser maior que zero.", 400);

            var peca = await _catalogo.ObterPeca(item.PecaId, ct)
                       ?? throw new OficinaException($"Peça não encontrada: {item.PecaId}", 404);

            itensMaterial.Add(new OrcamentoItemMaterial(Domain.Oficina.Enums.TipoMaterial.Peca, peca.Id, item.Quantidade, peca.PrecoUnitario));
            total += item.Quantidade * peca.PrecoUnitario;
        }

        foreach (var item in req.Itens.Insumos)
        {
            if (item.Quantidade <= 0)
                throw new OficinaException("Quantidade de insumo deve ser maior que zero.", 400);

            var insumo = await _catalogo.ObterInsumo(item.InsumoId, ct)
                         ?? throw new OficinaException($"Insumo não encontrado: {item.InsumoId}", 404);

            itensMaterial.Add(new OrcamentoItemMaterial(Domain.Oficina.Enums.TipoMaterial.Insumo, insumo.Id, item.Quantidade, insumo.PrecoUnitario));
            total += item.Quantidade * insumo.PrecoUnitario;
        }

        return (total, itensServico, itensMaterial);
    }
}
