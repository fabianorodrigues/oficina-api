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
                var d = new string((doc ?? string.Empty).Where(char.IsDigit).ToArray());
                return d.Length is 11 or 14;
            })
            .WithMessage("CPF/CNPJ invalido.");

        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().Must(EmailValidation.EnderecoValido).MaximumLength(150).WithMessage("Email invalido.");
        RuleFor(x => x.Telefone).NotEmpty().MaximumLength(20);
    }
}
