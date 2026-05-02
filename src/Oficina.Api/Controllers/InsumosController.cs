using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Security;
using Oficina.Application.DTO.CatalogoEstoque;
using Oficina.Application.UseCases.CatalogoEstoque;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/insumos")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class InsumosController : ControllerBase
{
    private readonly CadastrarInsumoUseCase _cadastrar;
    private readonly ListarInsumosUseCase _listar;
    private readonly ObterInsumoUseCase _obter;
    private readonly AtualizarInsumoUseCase _atualizar;

    public InsumosController(
        CadastrarInsumoUseCase cadastrar,
        ListarInsumosUseCase listar,
        ObterInsumoUseCase obter,
        AtualizarInsumoUseCase atualizar)
    {
        _cadastrar = cadastrar;
        _listar = listar;
        _obter = obter;
        _atualizar = atualizar;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var insumos = await _listar.Executar(ct);
        return Ok(insumos.Select(v => new { v.Id, v.Descricao, v.PrecoUnitario }));
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarInsumoRequest req, CancellationToken ct)
    {
        var id = await _cadastrar.Executar(req.PrecoUnitario, req.Descricao, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var v = await _obter.Executar(id, ct);
        return Ok(new { v.Id, v.Descricao, v.PrecoUnitario });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarInsumoRequest req, CancellationToken ct)
    {
        await _atualizar.Executar(id, req.PrecoUnitario, req.Descricao, ct);
        return NoContent();
    }
}
