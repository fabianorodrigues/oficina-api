using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Domain.Cadastro;

namespace Oficina.Application.UseCases.Cadastro;

public class ListarClientesUseCase
{
    private readonly ICadastroRepository _repo;
    public ListarClientesUseCase(ICadastroRepository repo) => _repo = repo;

    public Task<IReadOnlyList<Cliente>> Executar(CancellationToken ct)
        => _repo.ListarClientes(ct);
}

public class ObterClienteUseCase
{
    private readonly ICadastroRepository _repo;
    public ObterClienteUseCase(ICadastroRepository repo) => _repo = repo;

    public async Task<Cliente> Executar(Guid id, CancellationToken ct)
        => await _repo.ObterCliente(id, ct) ?? throw new OficinaException("Cliente não encontrado.", 404);
}

public class ObterVeiculoUseCase
{
    private readonly ICadastroRepository _repo;
    public ObterVeiculoUseCase(ICadastroRepository repo) => _repo = repo;

    public async Task<Veiculo> Executar(Guid id, CancellationToken ct)
        => await _repo.ObterVeiculo(id, ct) ?? throw new OficinaException("Veículo não encontrado.", 404);
}

public class ListarVeiculosUseCase
{
    private readonly ICadastroRepository _repo;
    public ListarVeiculosUseCase(ICadastroRepository repo) => _repo = repo;

    public Task<IReadOnlyList<Veiculo>> Executar(CancellationToken ct)
        => _repo.ListarVeiculos(ct);
}

public class ListarVeiculosPorClienteUseCase
{
    private readonly ICadastroRepository _repo;
    public ListarVeiculosPorClienteUseCase(ICadastroRepository repo) => _repo = repo;

    public Task<IReadOnlyList<Veiculo>> Executar(Guid clienteId, CancellationToken ct)
        => _repo.ListarVeiculosPorCliente(clienteId, ct);
}
