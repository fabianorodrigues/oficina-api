using FluentValidation;
using Oficina.Application.DTO.Oficina;

namespace Oficina.Application.Validators.Oficina;

public class CriarOsCorretivaRequestValidator : AbstractValidator<CriarOsCorretivaRequest>
{
    public CriarOsCorretivaRequestValidator()
    {
        RuleFor(x => x.VeiculoId).NotEmpty();
    }
}
