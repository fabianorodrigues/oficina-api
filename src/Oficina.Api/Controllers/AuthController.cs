using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Application.DTO.Seguranca;
using Oficina.Application.UseCases.Seguranca;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AutenticarClienteUseCase _autenticarCliente;
    private readonly AutenticarFuncionarioUseCase _autenticarFuncionario;
    private readonly IJwtTokenService _jwt;

    public AuthController(
        AutenticarClienteUseCase autenticarCliente,
        AutenticarFuncionarioUseCase autenticarFuncionario,
        IJwtTokenService jwt)
    {
        _autenticarCliente = autenticarCliente;
        _autenticarFuncionario = autenticarFuncionario;
        _jwt = jwt;
    }

    [HttpPost("clientes")]
    public async Task<IActionResult> LoginCliente([FromBody] LoginClienteRequest request, CancellationToken ct)
    {
        var cliente = await _autenticarCliente.Executar(request.Cpf, ct);
        return Ok(new AuthTokenResponse
        {
            AccessToken = _jwt.GerarTokenCliente(cliente),
            Perfil = "Cliente",
            ClienteId = cliente.Id
        });
    }

    [HttpPost("funcionarios")]
    public async Task<IActionResult> LoginFuncionario([FromBody] LoginFuncionarioRequest request, CancellationToken ct)
    {
        var funcionario = await _autenticarFuncionario.Executar(request.Cpf, request.Senha, ct);
        return Ok(new AuthTokenResponse
        {
            AccessToken = _jwt.GerarTokenFuncionario(funcionario),
            Perfil = funcionario.Perfil.ToString(),
            FuncionarioId = funcionario.Id
        });
    }
}
