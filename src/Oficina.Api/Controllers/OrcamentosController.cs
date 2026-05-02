using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Security;
using Oficina.Application.UseCases.Oficina;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/orcamentos")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class OrcamentosController : ControllerBase
{
    private readonly ObterOrcamentoDetalhadoUseCase _obter;
    private readonly AprovarOrcamentoUseCase _aprovar;
    private readonly RecusarOrcamentoUseCase _recusar;

    public OrcamentosController(
        ObterOrcamentoDetalhadoUseCase obter,
        AprovarOrcamentoUseCase aprovar,
        RecusarOrcamentoUseCase recusar)
    {
        _obter = obter;
        _aprovar = aprovar;
        _recusar = recusar;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var o = await _obter.Executar(id, ct);
        return Ok(o);
    }

    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken ct)
    {
        await _aprovar.Executar(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/recusar")]
    public async Task<IActionResult> Recusar(Guid id, CancellationToken ct)
    {
        await _recusar.Executar(id, ct);
        return NoContent();
    }
}
