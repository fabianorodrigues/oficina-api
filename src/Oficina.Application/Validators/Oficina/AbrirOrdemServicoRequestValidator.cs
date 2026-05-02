using FluentValidation;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.Validators;

namespace Oficina.Application.Validators.Oficina;

public class AbrirOrdemServicoRequestValidator : AbstractValidator<AbrirOrdemServicoRequest>
{
    public AbrirOrdemServicoRequestValidator()
    {
        RuleFor(x => x.TipoManutencao)
            .Must(tipo => string.IsNullOrWhiteSpace(tipo) ||
                          string.Equals(tipo, "Preventiva", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(tipo, "Corretiva", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Tipo de manutencao invalido.");

        RuleFor(x => x.Cliente).NotNull().SetValidator(new ClienteAberturaRequestValidator());
        RuleFor(x => x.Veiculo).NotNull().SetValidator(new VeiculoAberturaRequestValidator());
        RuleFor(x => x.Itens).NotNull().SetValidator(new ItensAberturaRequestValidator());

        RuleFor(x => x.Itens.Servicos)
            .Must(x => x.Count > 0)
            .When(x => string.Equals(x.TipoManutencao, "Preventiva", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Abertura preventiva exige ao menos 1 servico.");

        RuleFor(x => x)
            .Must(x => x.Itens.Servicos.Count > 0 || (x.Itens.Pecas.Count == 0 && x.Itens.Insumos.Count == 0))
            .WithMessage("Pecas e insumos so podem ser informados quando houver servicos.");
    }
}

public class ClienteAberturaRequestValidator : AbstractValidator<ClienteAberturaRequest>
{
    public ClienteAberturaRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Documento)
            .NotEmpty()
            .Must(doc =>
            {
                var d = new string((doc ?? string.Empty).Where(char.IsDigit).ToArray());
                return d.Length is 11 or 14;
            })
            .WithMessage("CPF/CNPJ invalido.");
        RuleFor(x => x.Email).NotEmpty().Must(EmailValidation.EnderecoValido).MaximumLength(150).WithMessage("Email invalido.");
        RuleFor(x => x.Telefone).NotEmpty().MaximumLength(20);
    }
}

public class VeiculoAberturaRequestValidator : AbstractValidator<VeiculoAberturaRequest>
{
    public VeiculoAberturaRequestValidator()
    {
        RuleFor(x => x.Placa)
            .NotEmpty()
            .Must(p =>
            {
                var v = (p ?? string.Empty).Trim().ToUpperInvariant().Replace("-", "");
                var antigo = System.Text.RegularExpressions.Regex.IsMatch(v, "^[A-Z]{3}[0-9]{4}$");
                var mercosul = System.Text.RegularExpressions.Regex.IsMatch(v, "^[A-Z]{3}[0-9]{1}[A-Z]{1}[0-9]{2}$");
                return antigo || mercosul;
            })
            .WithMessage("Placa invalida.");

        RuleFor(x => x.Renavam)
            .NotEmpty()
            .Must(r => new string((r ?? string.Empty).Where(char.IsDigit).ToArray()).Length == 11)
            .WithMessage("RENAVAM invalido.");

        RuleFor(x => x.Modelo).NotNull().SetValidator(new ModeloAberturaRequestValidator());
    }
}

public class ModeloAberturaRequestValidator : AbstractValidator<ModeloAberturaRequest>
{
    public ModeloAberturaRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Marca).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ano).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
    }
}

public class ItensAberturaRequestValidator : AbstractValidator<ItensAberturaRequest>
{
    public ItensAberturaRequestValidator()
    {
        RuleForEach(x => x.Servicos).SetValidator(new ServicoAberturaRequestValidator());
        RuleForEach(x => x.Pecas).SetValidator(new PecaAberturaRequestValidator());
        RuleForEach(x => x.Insumos).SetValidator(new InsumoAberturaRequestValidator());
    }
}

public class ServicoAberturaRequestValidator : AbstractValidator<ServicoAberturaRequest>
{
    public ServicoAberturaRequestValidator()
    {
        RuleFor(x => x.ServicoId).NotEmpty();
    }
}

public class PecaAberturaRequestValidator : AbstractValidator<PecaAberturaRequest>
{
    public PecaAberturaRequestValidator()
    {
        RuleFor(x => x.PecaId).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}

public class InsumoAberturaRequestValidator : AbstractValidator<InsumoAberturaRequest>
{
    public InsumoAberturaRequestValidator()
    {
        RuleFor(x => x.InsumoId).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}
