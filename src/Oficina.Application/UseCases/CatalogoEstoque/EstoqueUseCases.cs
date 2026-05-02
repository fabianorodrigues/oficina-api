using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.DTO.CatalogoEstoque;
using Oficina.Application.Shared;

namespace Oficina.Application.UseCases.CatalogoEstoque;

public class ListarEstoqueUseCase
{
    private readonly ICatalogoEstoqueRepository _repo;
    public ListarEstoqueUseCase(ICatalogoEstoqueRepository repo) => _repo = repo;

    public async Task<EstoqueResponse> Executar(CancellationToken ct)
    {
        var pecas = await _repo.ListarEstoquePecas(ct);
        var insumos = await _repo.ListarEstoqueInsumos(ct);

        return new EstoqueResponse
        {
            Pecas = pecas.Select(x => new EstoquePecaResponse
            {
                PecaId = x.PecaId,
                Quantidade = x.Quantidade
            }).ToList(),
            Insumos = insumos.Select(x => new EstoqueInsumoResponse
            {
                InsumoId = x.InsumoId,
                Quantidade = x.Quantidade
            }).ToList()
        };
    }
}

public class ObterEstoquePecaUseCase
{
    private readonly ICatalogoEstoqueRepository _repo;
    public ObterEstoquePecaUseCase(ICatalogoEstoqueRepository repo) => _repo = repo;

    public async Task<EstoquePecaResponse> Executar(Guid pecaId, CancellationToken ct)
    {
        var estoque = await _repo.ObterEstoquePeca(pecaId, ct)
                      ?? throw new OficinaException("Estoque da peça não encontrado.", 404);

        var peca = await _repo.ObterPeca(pecaId, ct)
                   ?? throw new OficinaException("Peça não encontrada.", 404);

        return new EstoquePecaResponse
        {
            PecaId = pecaId,
            Descricao = peca.Descricao,
            Quantidade = estoque.Quantidade
        };
    }
}

public class ObterEstoqueInsumoUseCase
{
    private readonly ICatalogoEstoqueRepository _repo;
    public ObterEstoqueInsumoUseCase(ICatalogoEstoqueRepository repo) => _repo = repo;

    public async Task<EstoqueInsumoResponse> Executar(Guid insumoId, CancellationToken ct)
    {
        var estoque = await _repo.ObterEstoqueInsumo(insumoId, ct)
                      ?? throw new OficinaException("Estoque do insumo não encontrado.", 404);

        var insumo = await _repo.ObterInsumo(insumoId, ct)
                     ?? throw new OficinaException("Insumo não encontrado.", 404);

        return new EstoqueInsumoResponse
        {
            InsumoId = insumoId,
            Descricao = insumo.Descricao,
            Quantidade = estoque.Quantidade
        };
    }
}

public class AjustarEstoquePecaUseCase
{
    private readonly ICatalogoEstoqueRepository _repo;
    public AjustarEstoquePecaUseCase(ICatalogoEstoqueRepository repo) => _repo = repo;

    public async Task Executar(Guid pecaId, int quantidade, CancellationToken ct)
    {
        var estoque = await _repo.ObterEstoquePeca(pecaId, ct);
        if (estoque is null) throw new OficinaException("Estoque da peça não encontrado.", 404);

        estoque.Ajustar(quantidade);
        await _repo.Salvar(ct);
    }
}

public class AjustarEstoqueInsumoUseCase
{
    private readonly ICatalogoEstoqueRepository _repo;
    public AjustarEstoqueInsumoUseCase(ICatalogoEstoqueRepository repo) => _repo = repo;

    public async Task Executar(Guid insumoId, int quantidade, CancellationToken ct)
    {
        var estoque = await _repo.ObterEstoqueInsumo(insumoId, ct);
        if (estoque is null) throw new OficinaException("Estoque do insumo não encontrado.", 404);

        estoque.Ajustar(quantidade);
        await _repo.Salvar(ct);
    }
}
