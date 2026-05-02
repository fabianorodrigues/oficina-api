using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.UseCases.Oficina;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/orcamentos/acoes-externas")]
[AllowAnonymous]
public class OrcamentosAcoesExternasController : ControllerBase
{
    private readonly ProcessarAcaoExternaOrcamentoUseCase _processarAcaoExterna;

    public OrcamentosAcoesExternasController(ProcessarAcaoExternaOrcamentoUseCase processarAcaoExterna)
    {
        _processarAcaoExterna = processarAcaoExterna;
    }

    [HttpGet("aprovar")]
    public Task<IActionResult> Aprovar([FromQuery] string token, CancellationToken ct)
        => Processar(token, AcaoExternaOrcamento.Aprovar, ct);

    [HttpGet("recusar")]
    public Task<IActionResult> Recusar([FromQuery] string token, CancellationToken ct)
        => Processar(token, AcaoExternaOrcamento.Recusar, ct);

    private async Task<IActionResult> Processar(string token, AcaoExternaOrcamento acao, CancellationToken ct)
    {
        var resultado = await _processarAcaoExterna.Executar(
            new ProcessarAcaoExternaOrcamentoRequest
            {
                Token = token,
                Acao = acao
            },
            ct);

        if (resultado.Sucesso) return Ok(resultado);

        return resultado.Codigo switch
        {
            "token_invalido" => BadRequest(resultado),
            "link_invalido" => NotFound(resultado),
            "link_expirado" => StatusCode(StatusCodes.Status410Gone, resultado),
            "acao_ja_processada" => Conflict(resultado),
            _ => BadRequest(resultado)
        };
    }
}
