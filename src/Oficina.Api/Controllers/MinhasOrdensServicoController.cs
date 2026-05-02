using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Security;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Application.UseCases.Oficina;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/minhas-ordens-servico")]
[Authorize(Policy = Policies.ClienteOnly)]
public class MinhasOrdensServicoController : ControllerBase
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IClienteOwnershipService _ownership;
    private readonly ListarMinhasOrdensServicoUseCase _listar;
    private readonly ObterOrdemServicoDetalhadaUseCase _obter;
    private readonly ObterStatusOrdemServicoUseCase _obterStatus;

    public MinhasOrdensServicoController(
        IUsuarioAtual usuarioAtual,
        IClienteOwnershipService ownership,
        ListarMinhasOrdensServicoUseCase listar,
        ObterOrdemServicoDetalhadaUseCase obter,
        ObterStatusOrdemServicoUseCase obterStatus)
    {
        _usuarioAtual = usuarioAtual;
        _ownership = ownership;
        _listar = listar;
        _obter = obter;
        _obterStatus = obterStatus;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok(await _listar.Executar(_usuarioAtual.ClienteId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
    {
        await _ownership.GarantirOrdemServico(_usuarioAtual.ClienteId, id, ct);
        return Ok(await _obter.Executar(id, ct));
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> ObterStatus(Guid id, CancellationToken ct)
    {
        await _ownership.GarantirOrdemServico(_usuarioAtual.ClienteId, id, ct);
        return Ok(await _obterStatus.Executar(id, ct));
    }
}
