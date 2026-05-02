using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Security;
using Oficina.Application.DTO.CatalogoEstoque;
using Oficina.Application.UseCases.CatalogoEstoque;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/pecas")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class PecasController : ControllerBase
{
    private readonly CadastrarPecaUseCase _cadastrar;
    private readonly ListarPecasUseCase _listar;
    private readonly ObterPecaUseCase _obter;
    private readonly AtualizarPecaUseCase _atualizar;

    public PecasController(
        CadastrarPecaUseCase cadastrar,
        ListarPecasUseCase listar,
        ObterPecaUseCase obter,
        AtualizarPecaUseCase atualizar)
    {
        _cadastrar = cadastrar;
        _listar = listar;
        _obter = obter;
        _atualizar = atualizar;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var pecas = await _listar.Executar(ct);
        return Ok(pecas.Select(v => new { v.Id, v.Descricao, v.PrecoUnitario }));
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarPecaRequest req, CancellationToken ct)
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
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarPecaRequest req, CancellationToken ct)
    {
        await _atualizar.Executar(id, req.PrecoUnitario, req.Descricao, ct);
        return NoContent();
    }
}
