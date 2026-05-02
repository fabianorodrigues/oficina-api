using FluentValidation;
using Oficina.Application.DTO.Cadastro;

namespace Oficina.Application.Validators.Cadastro;

public class AtualizarVeiculoRequestValidator : AbstractValidator<AtualizarVeiculoRequest>
{
    public AtualizarVeiculoRequestValidator()
    {
        RuleFor(x => x.Placa).NotEmpty();
        RuleFor(x => x.Renavam)
            .NotEmpty()
            .Must(r => new string((r ?? "").Where(char.IsDigit).ToArray()).Length == 11)
            .WithMessage("RENAVAM inválido.");

        RuleFor(x => x.Modelo).NotNull();
        When(x => x.Modelo is not null, () =>
        {
            RuleFor(x => x.Modelo!.Descricao).NotEmpty().MaximumLength(80);
            RuleFor(x => x.Modelo!.Marca).NotEmpty().MaximumLength(60);
            RuleFor(x => x.Modelo!.Ano).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        });
    }
}
