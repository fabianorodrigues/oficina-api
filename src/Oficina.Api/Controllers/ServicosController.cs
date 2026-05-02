using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Security;
using Oficina.Application.DTO.CatalogoEstoque;
using Oficina.Application.UseCases.CatalogoEstoque;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/servicos")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class ServicosController : ControllerBase
{
    private readonly CadastrarServicoUseCase _cadastrar;
    private readonly ListarServicosUseCase _listar;
    private readonly ObterServicoUseCase _obter;
    private readonly AtualizarServicoUseCase _atualizar;

    public ServicosController(
        CadastrarServicoUseCase cadastrar,
        ListarServicosUseCase listar,
        ObterServicoUseCase obter,
        AtualizarServicoUseCase atualizar)
    {
        _cadastrar = cadastrar;
        _listar = listar;
        _obter = obter;
        _atualizar = atualizar;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var servicos = await _listar.Executar(ct);
        return Ok(servicos.Select(Mapear));
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarServicoRequest req, CancellationToken ct)
    {
        var pecas = req.Pecas?.Select(x => (x.Id, x.Quantidade));
        var insumos = req.Insumos?.Select(x => (x.Id, x.Quantidade));

        var id = await _cadastrar.Executar(req.MaoDeObra, pecas, insumos, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var servico = await _obter.Executar(id, ct);
        return Ok(Mapear(servico));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CadastrarServicoRequest req, CancellationToken ct)
    {
        var pecas = req.Pecas?.Select(x => (x.Id, x.Quantidade));
        var insumos = req.Insumos?.Select(x => (x.Id, x.Quantidade));

        await _atualizar.Executar(req.MaoDeObra, id, pecas, insumos, ct);
        return NoContent();
    }

    private static object Mapear(Oficina.Domain.CatalogoEstoque.Servico servico)
        => new
        {
            servico.Id,
            servico.MaoDeObra,
            pecas = servico.Pecas.Select(x => new { id = x.PecaId, x.Quantidade }),
            insumos = servico.Insumos.Select(x => new { id = x.InsumoId, x.Quantidade })
        };
}
