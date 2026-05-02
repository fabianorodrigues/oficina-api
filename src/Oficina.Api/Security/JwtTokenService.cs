using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Seguranca;

namespace Oficina.Api.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public string GerarTokenCliente(Cliente cliente)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, cliente.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, PerfisAcesso.Cliente),
            new(ClaimsOficina.Cpf, cliente.Documento.Valor),
            new(ClaimsOficina.ClienteId, cliente.Id.ToString())
        };

        return GerarToken(claims);
    }

    public string GerarTokenFuncionario(Funcionario funcionario)
    {
        var perfil = funcionario.Perfil.ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, funcionario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, funcionario.Nome),
            new(ClaimTypes.Role, perfil),
            new(ClaimsOficina.Cpf, funcionario.Cpf),
            new(ClaimsOficina.FuncionarioId, funcionario.Id.ToString())
        };

        return GerarToken(claims);
    }

    private string GerarToken(IEnumerable<Claim> claims)
    {
        var secret = _configuration["Jwt:Secret"] ?? _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException("Jwt:Secret deve ter ao menos 32 caracteres.");

        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing.");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing.");
        var expMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"] ?? _configuration["Jwt:ExpMinutes"], out var m) ? m : 120;

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
