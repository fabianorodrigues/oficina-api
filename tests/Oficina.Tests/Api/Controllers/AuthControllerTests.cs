using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Oficina.Api.Controllers;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Application.DTO.Seguranca;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Seguranca;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Domain.Seguranca;
using Oficina.Domain.Seguranca.Enums;
using Xunit;

namespace Oficina.Tests.Api.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task LoginCpf_SemSenha_DeveAutenticarCliente()
    {
        var cliente = new Cliente(new DocumentoCpfCnpj("12345678909"), "Cliente", new Contato("cliente@teste.com", "11999999999"));
        var controller = CriarController(cliente: cliente);

        var result = await controller.LoginCpf(new LoginCpfRequest("123.456.789-09", null), CancellationToken.None);

        var response = ObterResponse(result);
        Assert.Equal("token-cliente", response.AccessToken);
        Assert.Equal(7200, response.ExpiresIn);
        Assert.Equal("Cliente", response.Perfil);
        Assert.Equal(cliente.Id, response.ClienteId);
        Assert.Null(response.FuncionarioId);
    }

    [Fact]
    public async Task LoginCpf_ComSenha_DeveAutenticarFuncionario()
    {
        var funcionario = new Funcionario("Funcionario", "12345678909", "hash", PerfilUsuarioInterno.Funcionario);
        var controller = CriarController(funcionario: funcionario, senhaValida: true);

        var result = await controller.LoginCpf(new LoginCpfRequest("12345678909", "SenhaTeste!123"), CancellationToken.None);

        var response = ObterResponse(result);
        Assert.Equal("token-funcionario", response.AccessToken);
        Assert.Equal(7200, response.ExpiresIn);
        Assert.Equal("Funcionario", response.Perfil);
        Assert.Equal(funcionario.Id, response.FuncionarioId);
        Assert.Null(response.ClienteId);
    }

    [Fact]
    public async Task LoginCpf_ComSenha_DeveAutenticarAdmin()
    {
        var admin = new Funcionario("Admin", "12345678909", "hash", PerfilUsuarioInterno.Admin);
        var controller = CriarController(funcionario: admin, senhaValida: true);

        var result = await controller.LoginCpf(new LoginCpfRequest("12345678909", "SenhaTeste!123"), CancellationToken.None);

        var response = ObterResponse(result);
        Assert.Equal("Admin", response.Perfil);
        Assert.Equal(admin.Id, response.FuncionarioId);
    }

    [Fact]
    public async Task LoginCpf_CpfInexistente_DeveFalharCom401()
    {
        var controller = CriarController();

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            controller.LoginCpf(new LoginCpfRequest("12345678909", null), CancellationToken.None));

        Assert.Equal(401, ex.StatusHttp);
    }

    [Fact]
    public async Task LoginCpf_SenhaInvalida_DeveFalharCom401()
    {
        var funcionario = new Funcionario("Funcionario", "12345678909", "hash", PerfilUsuarioInterno.Funcionario);
        var controller = CriarController(funcionario: funcionario, senhaValida: false);

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            controller.LoginCpf(new LoginCpfRequest("12345678909", "errada"), CancellationToken.None));

        Assert.Equal(401, ex.StatusHttp);
    }

    private static AuthController CriarController(Cliente? cliente = null, Funcionario? funcionario = null, bool senhaValida = false)
    {
        var cadastroRepo = new Mock<ICadastroRepository>();
        var funcionarioRepo = new Mock<IFuncionarioRepository>();
        var password = new Mock<IPasswordHashService>();
        var jwt = new Mock<IJwtTokenService>();

        cadastroRepo
            .Setup(x => x.ObterClientePorDocumento("12345678909", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        funcionarioRepo
            .Setup(x => x.ObterPorCpf("12345678909", It.IsAny<CancellationToken>()))
            .ReturnsAsync(funcionario);

        password
            .Setup(x => x.Verificar("hash", It.IsAny<string>()))
            .Returns(senhaValida);

        if (cliente is not null)
            jwt.Setup(x => x.GerarTokenCliente(cliente)).Returns("token-cliente");

        if (funcionario is not null)
            jwt.Setup(x => x.GerarTokenFuncionario(funcionario)).Returns("token-funcionario");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ExpirationMinutes"] = "120"
            })
            .Build();

        return new AuthController(
            new AutenticarClienteUseCase(cadastroRepo.Object),
            new AutenticarFuncionarioUseCase(funcionarioRepo.Object, password.Object),
            jwt.Object,
            configuration);
    }

    private static AuthTokenResponse ObterResponse(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<AuthTokenResponse>(ok.Value);
    }
}
