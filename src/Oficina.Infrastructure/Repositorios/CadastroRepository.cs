using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Domain.Cadastro;
using Oficina.Infrastructure.Persistencia;

namespace Oficina.Infrastructure.Repositorios;

public class CadastroRepository : ICadastroRepository
{
    private readonly OficinaDbContext _db;
    public CadastroRepository(OficinaDbContext db) => _db = db;

    public async Task<IReadOnlyList<Cliente>> ListarClientes(CancellationToken ct)
        => await _db.Clientes.OrderBy(x => x.Nome).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<Cliente?> ObterCliente(Guid id, CancellationToken ct)
        => _db.Clientes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct)
        => _db.Clientes.AnyAsync(x => x.Documento.Valor == cpfCnpjNormalizado, ct);

    public Task<Cliente?> ObterClientePorDocumento(string cpfCnpjNormalizado, CancellationToken ct)
        => _db.Clientes.FirstOrDefaultAsync(x => x.Documento.Valor == cpfCnpjNormalizado, ct);

    public Task AdicionarCliente(Cliente cliente, CancellationToken ct)
        => _db.Clientes.AddAsync(cliente, ct).AsTask();

    public async Task<IReadOnlyList<Veiculo>> ListarVeiculos(CancellationToken ct)
        => await _db.Veiculos.OrderBy(x => x.Placa.Valor).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<Veiculo?> ObterVeiculo(Guid id, CancellationToken ct)
        => _db.Veiculos.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Veiculo>> ListarVeiculosPorCliente(Guid clienteId, CancellationToken ct)
        => await _db.Veiculos.Where(x => x.ClienteId == clienteId).ToListAsync(ct);

    public Task<bool> ExisteVeiculoPorPlaca(string placaNormalizada, CancellationToken ct)
        => _db.Veiculos.AnyAsync(x => x.Placa.Valor == placaNormalizada, ct);

    public Task<Veiculo?> ObterVeiculoPorPlaca(string placaNormalizada, CancellationToken ct)
        => _db.Veiculos.FirstOrDefaultAsync(x => x.Placa.Valor == placaNormalizada, ct);

    public Task AdicionarVeiculo(Veiculo veiculo, CancellationToken ct)
        => _db.Veiculos.AddAsync(veiculo, ct).AsTask();

    public Task Salvar(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
