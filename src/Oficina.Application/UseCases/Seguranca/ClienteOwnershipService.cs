using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Application.Shared;

namespace Oficina.Application.UseCases.Seguranca;

public class ClienteOwnershipService : IClienteOwnershipService
{
    private readonly ICadastroRepository _cadastro;
    private readonly IOficinaRepository _oficina;

    public ClienteOwnershipService(ICadastroRepository cadastro, IOficinaRepository oficina)
    {
        _cadastro = cadastro;
        _oficina = oficina;
    }

    public Task GarantirCliente(Guid clienteIdAutenticado, Guid clienteIdRecurso, CancellationToken ct)
    {
        if (clienteIdAutenticado != clienteIdRecurso)
            throw new OficinaException("Acesso negado ao recurso solicitado.", 403);

        return Task.CompletedTask;
    }

    public async Task GarantirVeiculo(Guid clienteIdAutenticado, Guid veiculoId, CancellationToken ct)
    {
        var veiculo = await _cadastro.ObterVeiculo(veiculoId, ct)
                      ?? throw new OficinaException("Veiculo nao encontrado.", 404);

        await GarantirCliente(clienteIdAutenticado, veiculo.ClienteId, ct);
    }

    public async Task GarantirOrdemServico(Guid clienteIdAutenticado, Guid ordemServicoId, CancellationToken ct)
    {
        var os = await _oficina.ObterOrdemServico(ordemServicoId, ct)
                 ?? throw new OficinaException("Ordem de servico nao encontrada.", 404);

        await GarantirVeiculo(clienteIdAutenticado, os.VeiculoId, ct);
    }

    public async Task GarantirOrcamento(Guid clienteIdAutenticado, Guid orcamentoId, CancellationToken ct)
    {
        var orcamento = await _oficina.ObterOrcamento(orcamentoId, ct)
                       ?? throw new OficinaException("Orcamento nao encontrado.", 404);

        await GarantirOrdemServico(clienteIdAutenticado, orcamento.OrdemServicoId, ct);
    }
}
