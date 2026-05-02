using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Application.DTO.Oficina;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.UseCases.Oficina;

public class ListarMinhasOrdensServicoUseCase
{
    private readonly ICadastroRepository _cadastro;
    private readonly IOficinaRepository _oficina;

    public ListarMinhasOrdensServicoUseCase(ICadastroRepository cadastro, IOficinaRepository oficina)
    {
        _cadastro = cadastro;
        _oficina = oficina;
    }

    public async Task<IReadOnlyList<OrdemServicoListaItemResponse>> Executar(Guid clienteId, CancellationToken ct)
    {
        var veiculos = await _cadastro.ListarVeiculosPorCliente(clienteId, ct);
        var veiculoIds = veiculos.Select(x => x.Id).ToList();
        if (veiculoIds.Count == 0) return [];

        var ordens = await _oficina.ListarOrdensServicoPorVeiculos(veiculoIds, ct);
        return ordens
            .OrderByDescending(os => os.DataCriacao)
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
}

public class AprovarMeuOrcamentoUseCase
{
    private readonly IClienteOwnershipService _ownership;
    private readonly AprovarOrcamentoUseCase _aprovar;

    public AprovarMeuOrcamentoUseCase(IClienteOwnershipService ownership, AprovarOrcamentoUseCase aprovar)
    {
        _ownership = ownership;
        _aprovar = aprovar;
    }

    public async Task Executar(Guid clienteId, Guid orcamentoId, CancellationToken ct)
    {
        await _ownership.GarantirOrcamento(clienteId, orcamentoId, ct);
        await _aprovar.Executar(orcamentoId, ct, OrigemAtualizacaoStatusOs.Externa);
    }
}

public class RecusarMeuOrcamentoUseCase
{
    private readonly IClienteOwnershipService _ownership;
    private readonly RecusarOrcamentoUseCase _recusar;

    public RecusarMeuOrcamentoUseCase(IClienteOwnershipService ownership, RecusarOrcamentoUseCase recusar)
    {
        _ownership = ownership;
        _recusar = recusar;
    }

    public async Task Executar(Guid clienteId, Guid orcamentoId, CancellationToken ct)
    {
        await _ownership.GarantirOrcamento(clienteId, orcamentoId, ct);
        await _recusar.Executar(orcamentoId, ct, OrigemAtualizacaoStatusOs.Externa);
    }
}
