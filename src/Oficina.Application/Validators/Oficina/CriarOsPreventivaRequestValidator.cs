using FluentValidation;
using Oficina.Application.DTO.Oficina;

namespace Oficina.Application.Validators.Oficina;

public class CriarOsPreventivaRequestValidator : AbstractValidator<CriarOsPreventivaRequest>
{
    public CriarOsPreventivaRequestValidator()
    {
        RuleFor(x => x.VeiculoId).NotEmpty();
        RuleFor(x => x.ServicoIds)
            .NotNull()
            .Must(x => x.Count > 0)
            .WithMessage("Informe ao menos 1 serviço.");
    }
}
