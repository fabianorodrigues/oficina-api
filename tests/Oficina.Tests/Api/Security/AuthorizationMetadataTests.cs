using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Controllers;
using Oficina.Api.Security;
using Xunit;

namespace Oficina.Tests.Api.Security;

public class AuthorizationMetadataTests
{
    [Fact]
    public void AuthController_DeveSerAnonimo()
    {
        Assert.NotNull(Attribute.GetCustomAttribute(typeof(AuthController), typeof(AllowAnonymousAttribute)));
    }

    [Fact]
    public void AdminFuncionariosController_DeveSerAdminOnly()
    {
        var attr = Assert.IsType<AuthorizeAttribute>(
            Attribute.GetCustomAttribute(typeof(AdminFuncionariosController), typeof(AuthorizeAttribute)));

        Assert.Equal(Policies.AdminOnly, attr.Policy);
    }

    [Fact]
    public void MinhasOrdensServicoController_DeveSerClienteOnly()
    {
        var attr = Assert.IsType<AuthorizeAttribute>(
            Attribute.GetCustomAttribute(typeof(MinhasOrdensServicoController), typeof(AuthorizeAttribute)));

        Assert.Equal(Policies.ClienteOnly, attr.Policy);
    }

    [Fact]
    public void OrcamentosController_DeveSerFuncionarioOuAdmin()
    {
        var attr = Assert.IsType<AuthorizeAttribute>(
            Attribute.GetCustomAttribute(typeof(OrcamentosController), typeof(AuthorizeAttribute)));

        Assert.Equal(Policies.FuncionarioOuAdmin, attr.Policy);
    }

    [Fact]
    public void AuthController_DeveExporRotasPlurais()
    {
        var cliente = typeof(AuthController).GetMethod(nameof(AuthController.LoginCliente))!;
        var funcionario = typeof(AuthController).GetMethod(nameof(AuthController.LoginFuncionario))!;

        Assert.Equal("clientes", cliente.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true).Cast<HttpPostAttribute>().Single().Template);
        Assert.Equal("funcionarios", funcionario.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true).Cast<HttpPostAttribute>().Single().Template);
    }

    [Fact]
    public void OrdensServicoDiagnostico_DeveSerSingular()
    {
        var metodo = typeof(OrdensServicoController).GetMethod(nameof(OrdensServicoController.RegistrarDiagnostico))!;

        Assert.Equal("{id:guid}/diagnostico", metodo.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true).Cast<HttpPostAttribute>().Single().Template);
    }
}
