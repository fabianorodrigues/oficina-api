using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Security;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.UseCases.Oficina;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/ordens-servico")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class OrdensServicoController : ControllerBase
{
    private readonly AbrirOrdemServicoUseCase _abrirOrdemServico;
    private readonly RegistrarDiagnosticoUseCase _registrarDiagnostico;
    private readonly ClassificarOrdemServicoUseCase _classificar;
    private readonly ObterStatusOrdemServicoUseCase _obterStatus;
    private readonly ObterOrdemServicoDetalhadaUseCase _obter;
    private readonly ListarOrdensServicoUseCase _listar;
    private readonly FinalizarOrdemServicoUseCase _finalizar;
    private readonly EntregarOrdemServicoUseCase _entregar;

    public OrdensServicoController(
        AbrirOrdemServicoUseCase abrirOrdemServico,
        RegistrarDiagnosticoUseCase registrarDiagnostico,
        ClassificarOrdemServicoUseCase classificar,
        ObterStatusOrdemServicoUseCase obterStatus,
        ObterOrdemServicoDetalhadaUseCase obter,
        ListarOrdensServicoUseCase listar,
        FinalizarOrdemServicoUseCase finalizar,
        EntregarOrdemServicoUseCase entregar)
    {
        _abrirOrdemServico = abrirOrdemServico;
        _registrarDiagnostico = registrarDiagnostico;
        _classificar = classificar;
        _obterStatus = obterStatus;
        _obter = obter;
        _listar = listar;
        _finalizar = finalizar;
        _entregar = entregar;
    }


    [HttpPost]
    public async Task<IActionResult> Abrir([FromBody] AbrirOrdemServicoRequest req, CancellationToken ct)
    {
        var resultado = await _abrirOrdemServico.Executar(req, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado);
    }

    [HttpPost("{id:guid}/classificar")]
    public async Task<IActionResult> Classificar(Guid id, [FromBody] ClassificarOrdemServicoRequest req, CancellationToken ct)
    {
        await _classificar.Executar(id, req.TipoManutencao, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/diagnostico")]
    public async Task<IActionResult> RegistrarDiagnostico(Guid id, [FromBody] RegistrarDiagnosticoRequest req, CancellationToken ct)
    {
        var response = await _registrarDiagnostico.Executar(id, req.Descricao, req.ServicoIds, ct);
        return Ok(response);
    }


    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> ObterStatus(Guid id, CancellationToken ct)
    {
        var status = await _obterStatus.Executar(id, ct);
        return Ok(status);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var detalhe = await _obter.Executar(id, ct);

        return Ok(detalhe);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var lista = await _listar.Executar(ct);
        return Ok(lista);
    }

    [HttpPost("{id:guid}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid id, CancellationToken ct)
    {
        await _finalizar.Executar(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/entregar")]
    public async Task<IActionResult> Entregar(Guid id, CancellationToken ct)
    {
        await _entregar.Executar(id, ct);
        return NoContent();
    }
}
