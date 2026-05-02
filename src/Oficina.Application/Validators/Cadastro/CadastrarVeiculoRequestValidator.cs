using FluentValidation;
using Oficina.Application.DTO.Cadastro;

namespace Oficina.Application.Validators.Cadastro;

public class CadastrarVeiculoRequestValidator : AbstractValidator<CadastrarVeiculoRequest>
{
    public CadastrarVeiculoRequestValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();

        RuleFor(x => x.Placa)
            .NotEmpty()
            .Must(p =>
            {
                var v = (p ?? string.Empty).Trim().ToUpperInvariant().Replace("-", "");
                var antigo = System.Text.RegularExpressions.Regex.IsMatch(v, "^[A-Z]{3}[0-9]{4}$");
                var mercosul = System.Text.RegularExpressions.Regex.IsMatch(v, "^[A-Z]{3}[0-9]{1}[A-Z]{1}[0-9]{2}$");
                return antigo || mercosul;
            })
            .WithMessage("Placa inválida.");

        RuleFor(x => x.Renavam)
            .NotEmpty()
            .Must(r =>
            {
                var d = new string((r ?? string.Empty).Where(char.IsDigit).ToArray());
                return d.Length == 11;
            })
            .WithMessage("RENAVAM inválido.");

        RuleFor(x => x.Modelo)
            .NotNull()
            .WithMessage("Modelo e obrigatório.");

        When(x => x.Modelo != null, () =>
        {
            RuleFor(x => x.Modelo.Descricao)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Modelo.Marca)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Modelo.Ano)
                .GreaterThan(0);
        });
    }
}
