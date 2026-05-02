using FluentValidation;
using Oficina.Application.DTO.Oficina;

namespace Oficina.Application.Validators.Oficina;

public class RegistrarDiagnosticoRequestValidator : AbstractValidator<RegistrarDiagnosticoRequest>
{
    public RegistrarDiagnosticoRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ServicoIds)
            .NotNull()
            .Must(x => x.Count > 0)
            .WithMessage("Informe ao menos 1 serviço.");
    }
}
