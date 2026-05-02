using FluentValidation;
using Oficina.Application.DTO.Cadastro;

namespace Oficina.Application.Validators.Cadastro;

public class AtualizarClienteRequestValidator : AbstractValidator<AtualizarClienteRequest>
{
    public AtualizarClienteRequestValidator()
    {
        RuleFor(x => x.CpfCnpj)
            .NotEmpty()
            .Must(doc =>
            {
                var d = new string((doc ?? "").Where(char.IsDigit).ToArray());
                return d.Length is 11 or 14;
            })
            .WithMessage("CPF/CNPJ inválido.");
    }
}
