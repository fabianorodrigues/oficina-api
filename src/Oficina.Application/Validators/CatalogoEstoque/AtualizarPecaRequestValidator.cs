using FluentValidation;
using Oficina.Application.DTO.CatalogoEstoque;

namespace Oficina.Application.Validators.CatalogoEstoque;

public class AtualizarPecaRequestValidator : AbstractValidator<AtualizarPecaRequest>
{
    public AtualizarPecaRequestValidator()
    {
        RuleFor(x => x.PrecoUnitario).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Descricao)
            .NotEmpty()
            .MaximumLength(200);
    }
}
