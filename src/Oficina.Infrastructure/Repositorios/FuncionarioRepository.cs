using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Domain.Seguranca;
using Oficina.Infrastructure.Persistencia;

namespace Oficina.Infrastructure.Repositorios;

public class FuncionarioRepository : IFuncionarioRepository
{
    private readonly OficinaDbContext _db;

    public FuncionarioRepository(OficinaDbContext db) => _db = db;

    public Task<Funcionario?> ObterPorId(Guid id, CancellationToken ct)
        => _db.Funcionarios.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Funcionario?> ObterPorCpf(string cpfNormalizado, CancellationToken ct)
        => _db.Funcionarios.FirstOrDefaultAsync(x => x.Cpf == cpfNormalizado, ct);

    public Task<bool> ExistePorCpf(string cpfNormalizado, CancellationToken ct)
        => _db.Funcionarios.AnyAsync(x => x.Cpf == cpfNormalizado, ct);

    public async Task<IReadOnlyList<Funcionario>> Listar(CancellationToken ct)
        => await _db.Funcionarios.OrderBy(x => x.Nome).ThenBy(x => x.Id).ToListAsync(ct);

    public Task Adicionar(Funcionario funcionario, CancellationToken ct)
        => _db.Funcionarios.AddAsync(funcionario, ct).AsTask();

    public Task Salvar(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
