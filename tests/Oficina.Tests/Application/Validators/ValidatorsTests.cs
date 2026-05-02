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
    public void AtualizarClienteValidator_deve_rejeitar_email_invalido()
    {
        var v = new AtualizarClienteRequestValidator();
        var r = v.Validate(new AtualizarClienteRequest("39053344705", "Joao", "joao.@email.com", "11999999999"));

        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.PropertyName == nameof(AtualizarClienteRequest.Email));
    }

    [Fact]
    public void AbrirOrdemServicoValidator_deve_rejeitar_email_placa_renavam_e_quantidades_invalidas()
    {
        var v = new AbrirOrdemServicoRequestValidator();
        var r = v.Validate(new AbrirOrdemServicoRequest
        {
            Cliente = new ClienteAberturaRequest
            {
                Nome = "Joao",
                Documento = "39053344705",
                Email = "joao.@email.com",
                Telefone = "11999999999"
            },
            Veiculo = new VeiculoAberturaRequest
            {
                Placa = "ABCD",
                Renavam = "1234",
                Modelo = new ModeloAberturaRequest
                {
                    Descricao = "Corolla",
                    Marca = "Toyota",
                    Ano = 2020
                }
            },
            Itens = new ItensAberturaRequest
            {
                Pecas = [new PecaAberturaRequest { PecaId = Guid.NewGuid(), Quantidade = 0 }],
                Insumos = [new InsumoAberturaRequest { InsumoId = Guid.NewGuid(), Quantidade = -1 }]
            }
        });

        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.PropertyName == "Cliente.Email");
        Assert.Contains(r.Errors, e => e.PropertyName == "Veiculo.Placa");
        Assert.Contains(r.Errors, e => e.PropertyName == "Veiculo.Renavam");
        Assert.Contains(r.Errors, e => e.PropertyName == "Itens.Pecas[0].Quantidade");
        Assert.Contains(r.Errors, e => e.PropertyName == "Itens.Insumos[0].Quantidade");
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
