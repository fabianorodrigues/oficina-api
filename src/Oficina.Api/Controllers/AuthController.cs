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
    private readonly IConfiguration _configuration;

    public AuthController(
        AutenticarClienteUseCase autenticarCliente,
        AutenticarFuncionarioUseCase autenticarFuncionario,
        IJwtTokenService jwt,
        IConfiguration configuration)
    {
        _autenticarCliente = autenticarCliente;
        _autenticarFuncionario = autenticarFuncionario;
        _jwt = jwt;
        _configuration = configuration;
    }

    [HttpPost("cpf")]
    public async Task<IActionResult> LoginCpf([FromBody] LoginCpfRequest request, CancellationToken ct)
    {
        var expiresIn = ObterExpiracaoEmSegundos();

        if (string.IsNullOrWhiteSpace(request.Senha))
        {
            var cliente = await _autenticarCliente.Executar(request.Cpf, ct);
            return Ok(new AuthTokenResponse
            {
                AccessToken = _jwt.GerarTokenCliente(cliente),
                ExpiresIn = expiresIn,
                Perfil = "Cliente",
                ClienteId = cliente.Id
            });
        }

        var funcionario = await _autenticarFuncionario.Executar(request.Cpf, request.Senha, ct);
        return Ok(new AuthTokenResponse
        {
            AccessToken = _jwt.GerarTokenFuncionario(funcionario),
            ExpiresIn = expiresIn,
            Perfil = funcionario.Perfil.ToString(),
            FuncionarioId = funcionario.Id
        });
    }

    private int ObterExpiracaoEmSegundos()
    {
        var valor = _configuration["Jwt:ExpirationMinutes"] ?? _configuration["Jwt:ExpMinutes"];
        return int.TryParse(valor, out var minutos) ? minutos * 60 : 120 * 60;
    }
}
