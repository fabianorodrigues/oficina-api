using FluentValidation;
using Oficina.Application.DTO.CatalogoEstoque;

namespace Oficina.Application.Validators.CatalogoEstoque;

public class AtualizarInsumoRequestValidator : AbstractValidator<AtualizarInsumoRequest>
{
    public AtualizarInsumoRequestValidator()
    {
        RuleFor(x => x.PrecoUnitario).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Descricao)
            .NotEmpty()
            .MaximumLength(200);
    }
}
