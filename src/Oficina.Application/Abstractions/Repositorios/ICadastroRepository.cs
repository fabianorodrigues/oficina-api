using Oficina.Domain.Cadastro;

namespace Oficina.Application.Abstractions.Repositorios;

public interface ICadastroRepository
{
    Task<IReadOnlyList<Cliente>> ListarClientes(CancellationToken ct);
    Task<Cliente?> ObterCliente(Guid id, CancellationToken ct);
    Task<bool> ExisteClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct);
    Task<Cliente?> ObterClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct);
    Task AdicionarCliente(Cliente cliente, CancellationToken ct);

    Task<IReadOnlyList<Veiculo>> ListarVeiculos(CancellationToken ct);
    Task<Veiculo?> ObterVeiculo(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Veiculo>> ListarVeiculosPorCliente(Guid clienteId, CancellationToken ct);
    Task<bool> ExisteVeiculoPorPlaca(string placaNormalizada, CancellationToken ct);
    Task<Veiculo?> ObterVeiculoPorPlaca(string placaNormalizada, CancellationToken ct);
    Task AdicionarVeiculo(Veiculo veiculo, CancellationToken ct);

    Task Salvar(CancellationToken ct);
}
