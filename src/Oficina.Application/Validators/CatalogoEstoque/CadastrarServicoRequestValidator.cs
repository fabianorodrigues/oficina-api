using FluentValidation;
using Oficina.Application.DTO.CatalogoEstoque;

namespace Oficina.Application.Validators.CatalogoEstoque;

public class CadastrarServicoRequestValidator : AbstractValidator<CadastrarServicoRequest>
{
    public CadastrarServicoRequestValidator()
    {
        RuleFor(x => x.MaoDeObra).GreaterThanOrEqualTo(0);

        RuleForEach(x => x.Pecas)
            .SetValidator(new ItemRequeridoRequestValidator())
            .When(x => x.Pecas != null);

        RuleForEach(x => x.Insumos)
            .SetValidator(new ItemRequeridoRequestValidator())
            .When(x => x.Insumos != null);
    }
}

public class ItemRequeridoRequestValidator : AbstractValidator<ItemRequeridoRequest>
{
    public ItemRequeridoRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}
