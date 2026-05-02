using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Infrastructure.Persistencia;

namespace Oficina.Infrastructure.Repositorios;

public class CatalogoEstoqueRepository : ICatalogoEstoqueRepository
{
    private readonly OficinaDbContext _db;
    public CatalogoEstoqueRepository(OficinaDbContext db) => _db = db;

    public async Task<IReadOnlyList<Servico>> ListarServicos(CancellationToken ct)
        => await _db.Servicos
            .Include(x => x.Pecas)
            .Include(x => x.Insumos)
            .OrderBy(x => x.MaoDeObra)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

    public Task<Servico?> ObterServico(Guid id, CancellationToken ct)
        => _db.Servicos
              .Include(x => x.Pecas)
              .Include(x => x.Insumos)
              .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AdicionarServico(Servico servico, CancellationToken ct)
        => _db.Servicos.AddAsync(servico, ct).AsTask();

    public async Task<IReadOnlyList<Peca>> ListarPecas(CancellationToken ct)
        => await _db.Pecas.OrderBy(x => x.Descricao).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<Peca?> ObterPeca(Guid id, CancellationToken ct)
        => _db.Pecas.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AdicionarPeca(Peca peca, CancellationToken ct)
        => _db.Pecas.AddAsync(peca, ct).AsTask();

    public async Task<IReadOnlyList<Insumo>> ListarInsumos(CancellationToken ct)
        => await _db.Insumos.OrderBy(x => x.Descricao).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<Insumo?> ObterInsumo(Guid id, CancellationToken ct)
        => _db.Insumos.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AdicionarInsumo(Insumo insumo, CancellationToken ct)
        => _db.Insumos.AddAsync(insumo, ct).AsTask();

    public async Task<IReadOnlyList<EstoquePeca>> ListarEstoquePecas(CancellationToken ct)
        => await _db.EstoquePecas.OrderBy(x => x.PecaId).ToListAsync(ct);

    public Task<EstoquePeca?> ObterEstoquePeca(Guid pecaId, CancellationToken ct)
        => _db.EstoquePecas.FirstOrDefaultAsync(x => x.PecaId == pecaId, ct);

    public async Task<IReadOnlyList<EstoqueInsumo>> ListarEstoqueInsumos(CancellationToken ct)
        => await _db.EstoqueInsumos.OrderBy(x => x.InsumoId).ToListAsync(ct);

    public Task<EstoqueInsumo?> ObterEstoqueInsumo(Guid insumoId, CancellationToken ct)
        => _db.EstoqueInsumos.FirstOrDefaultAsync(x => x.InsumoId == insumoId, ct);

    public Task AdicionarEstoquePeca(EstoquePeca estoque, CancellationToken ct)
        => _db.EstoquePecas.AddAsync(estoque, ct).AsTask();

    public Task AdicionarEstoqueInsumo(EstoqueInsumo estoque, CancellationToken ct)
        => _db.EstoqueInsumos.AddAsync(estoque, ct).AsTask();

    public Task Salvar(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
