using FluentValidation;
using Oficina.Application.DTO.Cadastro;

namespace Oficina.Application.Validators.Cadastro;

public class CadastrarClienteRequestValidator : AbstractValidator<CadastrarClienteRequest>
{
    public CadastrarClienteRequestValidator()
    {
        RuleFor(x => x.CpfCnpj)
            .NotEmpty()
            .Must(doc =>
            {
                var d = new string((doc ?? "").Where(char.IsDigit).ToArray());
                return d.Length is 11 or 14;
            })
            .WithMessage("CPF/CNPJ inválido.");
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Telefone).NotEmpty().MaximumLength(20);

    }
}
