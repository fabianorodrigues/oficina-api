using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Api.Filters;
using Oficina.Application.DTO.Cadastro;
using Oficina.Application.Validators.Cadastro;
using Xunit;

namespace Oficina.Tests.Api.Filters;

public class FluentValidationActionFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_DeveRetornarBadRequest_QuandoDtoForInvalido()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<AtualizarClienteRequest>, AtualizarClienteRequestValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var actionContext = CriarActionContext(serviceProvider);
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>
            {
                ["req"] = new AtualizarClienteRequest("12345678909", "Joao", "joao.@email.com", "11999999999")
            },
            new object());
        var actionExecutada = false;
        var filtro = new FluentValidationActionFilter(serviceProvider);

        await filtro.OnActionExecutionAsync(executingContext, () =>
        {
            actionExecutada = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
        });

        Assert.False(actionExecutada);
        Assert.IsType<BadRequestObjectResult>(executingContext.Result);
    }

    private static ActionContext CriarActionContext(IServiceProvider serviceProvider)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
    }
}
