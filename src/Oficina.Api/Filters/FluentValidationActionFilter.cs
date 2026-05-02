using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Oficina.Api.Filters;

public class FluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationActionFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var erros = new Dictionary<string, string[]>();

        foreach (var argumento in context.ActionArguments.Values.Where(x => x is not null))
        {
            var tipoValidador = typeof(IValidator<>).MakeGenericType(argumento!.GetType());
            if (_serviceProvider.GetService(tipoValidador) is not IValidator validador)
                continue;

            var contextoValidacao = new ValidationContext<object>(argumento);
            var resultado = await validador.ValidateAsync(contextoValidacao, context.HttpContext.RequestAborted);

            foreach (var grupo in resultado.Errors.GroupBy(x => x.PropertyName))
                erros[grupo.Key] = grupo.Select(x => x.ErrorMessage).ToArray();
        }

        if (erros.Count > 0)
        {
            context.Result = new BadRequestObjectResult(new { erros });
            return;
        }

        await next();
    }
}
