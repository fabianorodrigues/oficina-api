using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Security;
using Oficina.Application.DTO.Cadastro;
using Oficina.Application.UseCases.Cadastro;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/veiculos")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class VeiculosController : ControllerBase
{
    private readonly CadastrarVeiculoUseCase _cadastrar;
    private readonly AtualizarVeiculoUseCase _atualizar;
    private readonly ListarVeiculosUseCase _listar;
    private readonly ObterVeiculoUseCase _obter;

    public VeiculosController(
        CadastrarVeiculoUseCase cadastrar,
        AtualizarVeiculoUseCase atualizar,
        ListarVeiculosUseCase listar,
        ObterVeiculoUseCase obter)
    {
        _cadastrar = cadastrar;
        _atualizar = atualizar;
        _listar = listar;
        _obter = obter;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var lista = await _listar.Executar(ct);
        return Ok(lista.Select(Mapear));
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarVeiculoRequest req, CancellationToken ct)
    {
        var id = await _cadastrar.Executar(req.ClienteId, req.Placa, req.Renavam, req.Modelo, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var v = await _obter.Executar(id, ct);
        return Ok(Mapear(v));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarVeiculoRequest req, CancellationToken ct)
    {
        await _atualizar.Executar(id, req.Placa, req.Renavam, req.Modelo, ct);
        return NoContent();
    }

    private static object Mapear(Oficina.Domain.Cadastro.Veiculo v)
        => new
        {
            v.Id,
            v.ClienteId,
            placa = v.Placa.Valor,
            renavam = v.Renavam.Valor,
            modelo = new { descricao = v.Modelo.Descricao, marca = v.Modelo.Marca, ano = v.Modelo.Ano }
        };
}
