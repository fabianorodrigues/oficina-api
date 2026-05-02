using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Security;
using Oficina.Application.UseCases.Oficina;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/relatorios")]
[Authorize(Policy = Policies.FuncionarioOuAdmin)]
public class RelatoriosController : ControllerBase
{
    private readonly RelatorioTempoMedioExecucaoUseCase _tempoMedio;

    public RelatoriosController(RelatorioTempoMedioExecucaoUseCase tempoMedio)
    {
        _tempoMedio = tempoMedio;
    }

    [HttpGet("tempo-medio-execucao")]
    public async Task<IActionResult> TempoMedioExecucao(CancellationToken ct)
    {
        var response = await _tempoMedio.Executar(ct);
        return Ok(response);
    }
}
