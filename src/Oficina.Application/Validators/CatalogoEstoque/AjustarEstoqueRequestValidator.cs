using FluentValidation;
using Oficina.Application.DTO.CatalogoEstoque;

namespace Oficina.Application.Validators.CatalogoEstoque;

public class AjustarEstoqueRequestValidator : AbstractValidator<AjustarEstoqueRequest>
{
    public AjustarEstoqueRequestValidator()
    {
        RuleFor(x => x.Quantidade).NotEqual(0);
    }
}
