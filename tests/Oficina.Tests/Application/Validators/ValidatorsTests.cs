using Oficina.Application.DTO.Cadastro;
using Oficina.Application.DTO.CatalogoEstoque;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.Validators.Cadastro;
using Oficina.Application.Validators.CatalogoEstoque;
using Oficina.Application.Validators.Oficina;
using Xunit;

namespace Oficina.Tests.Application.Validators;

public class ValidatorsTests
{
    [Fact]
    public void CadastrarClienteValidator_deve_rejeitar_campos_vazios()
    {
        var v = new CadastrarClienteRequestValidator();
        var r = v.Validate(new CadastrarClienteRequest(string.Empty, string.Empty, string.Empty, string.Empty));
        Assert.False(r.IsValid);
        Assert.True(r.Errors.Count >= 2);
    }

    [Fact]
    public void CadastrarVeiculoValidator_deve_rejeitar_campos_invalidos()
    {
        var v = new CadastrarVeiculoRequestValidator();
        var r = v.Validate(new CadastrarVeiculoRequest(Guid.Empty, "", "", new ModeloRequest("", "", 0)));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void CadastrarServicoValidator_deve_rejeitar_valor_negativo()
    {
        var v = new CadastrarServicoRequestValidator();
        var r = v.Validate(new CadastrarServicoRequest(-1m, new List<ItemRequeridoRequest>(), new List<ItemRequeridoRequest>()));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void RegistrarDiagnosticoValidator_deve_exigir_descricao_e_servicos()
    {
        var v = new RegistrarDiagnosticoRequestValidator();
        var r = v.Validate(new RegistrarDiagnosticoRequest(" ", new List<Guid>()));
        Assert.False(r.IsValid);
    }
}
