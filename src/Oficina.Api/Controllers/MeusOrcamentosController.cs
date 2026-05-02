using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Security;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Application.UseCases.Oficina;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/meus-orcamentos")]
[Authorize(Policy = Policies.ClienteOnly)]
public class MeusOrcamentosController : ControllerBase
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IClienteOwnershipService _ownership;
    private readonly ObterOrcamentoDetalhadoUseCase _obter;
    private readonly AprovarMeuOrcamentoUseCase _aprovar;
    private readonly RecusarMeuOrcamentoUseCase _recusar;

    public MeusOrcamentosController(
        IUsuarioAtual usuarioAtual,
        IClienteOwnershipService ownership,
        ObterOrcamentoDetalhadoUseCase obter,
        AprovarMeuOrcamentoUseCase aprovar,
        RecusarMeuOrcamentoUseCase recusar)
    {
        _usuarioAtual = usuarioAtual;
        _ownership = ownership;
        _obter = obter;
        _aprovar = aprovar;
        _recusar = recusar;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
    {
        await _ownership.GarantirOrcamento(_usuarioAtual.ClienteId, id, ct);
        return Ok(await _obter.Executar(id, ct));
    }

    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken ct)
    {
        await _aprovar.Executar(_usuarioAtual.ClienteId, id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/recusar")]
    public async Task<IActionResult> Recusar(Guid id, CancellationToken ct)
    {
        await _recusar.Executar(_usuarioAtual.ClienteId, id, ct);
        return NoContent();
    }
}
