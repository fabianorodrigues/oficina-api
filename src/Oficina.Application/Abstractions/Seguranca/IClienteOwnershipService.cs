namespace Oficina.Application.Abstractions.Seguranca;

public interface IClienteOwnershipService
{
    Task GarantirCliente(Guid clienteIdAutenticado, Guid clienteIdRecurso, CancellationToken ct);
    Task GarantirVeiculo(Guid clienteIdAutenticado, Guid veiculoId, CancellationToken ct);
    Task GarantirOrdemServico(Guid clienteIdAutenticado, Guid ordemServicoId, CancellationToken ct);
    Task GarantirOrcamento(Guid clienteIdAutenticado, Guid orcamentoId, CancellationToken ct);
}
