using Microsoft.EntityFrameworkCore;
using Oficina.Infrastructure.Persistencia;
using Oficina.Domain.Oficina;
using Oficina.Application.Abstractions.Repositorios;

namespace Oficina.Infrastructure.Repositorios;

public class OficinaRepository : IOficinaRepository
{
    private readonly OficinaDbContext _db;
    public OficinaRepository(OficinaDbContext db) => _db = db;

    public Task<OrdemServico?> ObterOrdemServico(Guid id, CancellationToken ct)
        => _db.OrdensServico
              .Include(x => x.ItensServico)
              .Include(x => x.Diagnostico)
              .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<OrdemServico>> ListarOrdensServico(CancellationToken ct)
        => await _db.OrdensServico
              .OrderByDescending(x => x.DataCriacao)
              .ToListAsync(ct);

    public async Task<IReadOnlyList<OrdemServico>> ListarOrdensServicoPorVeiculos(IReadOnlyCollection<Guid> veiculoIds, CancellationToken ct)
        => await _db.OrdensServico
              .Where(x => veiculoIds.Contains(x.VeiculoId))
              .OrderByDescending(x => x.DataCriacao)
              .ToListAsync(ct);

    public Task AdicionarOrdemServico(OrdemServico os, CancellationToken ct)
        => _db.OrdensServico.AddAsync(os, ct).AsTask();

    public Task<Orcamento?> ObterOrcamento(Guid id, CancellationToken ct)
        => _db.Orcamentos
              .Include(x => x.ItensServico)
              .Include(x => x.ItensMaterial)
              .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Orcamento?> ObterOrcamentoPorTokenAcaoExterna(string token, CancellationToken ct)
        => _db.Orcamentos
              .Include(x => x.ItensServico)
              .Include(x => x.ItensMaterial)
              .FirstOrDefaultAsync(x => x.TokenAcaoExterna == token, ct);

    public Task<Orcamento?> ObterOrcamentoPorOs(Guid ordemServicoId, CancellationToken ct)
        => _db.Orcamentos
              .Include(x => x.ItensServico)
              .Include(x => x.ItensMaterial)
              .FirstOrDefaultAsync(x => x.OrdemServicoId == ordemServicoId, ct);

    public Task AdicionarOrcamento(Orcamento orcamento, CancellationToken ct)
        => _db.Orcamentos.AddAsync(orcamento, ct).AsTask();

    public Task Salvar(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
