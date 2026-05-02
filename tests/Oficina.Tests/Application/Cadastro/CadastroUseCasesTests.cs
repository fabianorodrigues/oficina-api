using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.DTO.Cadastro;
using Oficina.Application.UseCases.Cadastro;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Xunit;

namespace Oficina.Tests.Application.Cadastro;

public class CadastroUseCasesTests
{
    [Fact]
    public async Task CadastrarCliente_deve_adicionar_e_salvar()
    {
        var repo = new Mock<ICadastroRepository>();
        var uc = new CadastrarClienteUseCase(repo.Object);

        var id = await uc.Executar("123.456.789-09", "João", "joao@email.com", "11999990000", CancellationToken.None);

        repo.Verify(r => r.AdicionarCliente(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.Salvar(It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task AtualizarCliente_deve_atualizar_nome_e_contato()
    {
        var repo = new Mock<ICadastroRepository>();
        var cliente = new Cliente(new DocumentoCpfCnpj("123.456.789-09"), "Nome antigo", new Contato("a@a.com", "11999990000"));
        repo.Setup(r => r.ObterCliente(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);

        var uc = new AtualizarClienteUseCase(repo.Object);
        await uc.Executar(cliente.Id, cliente.Documento.Valor, "Nome novo", "b@b.com", "11888887777", CancellationToken.None);

        Assert.Equal("Nome novo", cliente.Nome);
        Assert.Equal("b@b.com", cliente.Contato.Email);
        repo.Verify(r => r.Salvar(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task AtualizarVeiculo_deve_atualizar_modelo()
    {
        var repo = new Mock<ICadastroRepository>();
        var veiculo = new Veiculo(
            Guid.NewGuid(),
            new Placa("ABC1D23"),
            new Renavam("12345678901"),
            new Modelo("Civic", "Honda", 2020));

        repo.Setup(r => r.ObterVeiculo(veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);

        var uc = new AtualizarVeiculoUseCase(repo.Object);
        var modelo = new ModeloRequest("Corolla", "Toyota", 2022);
        await uc.Executar(veiculo.Id, veiculo.Placa.Valor, veiculo.Renavam.Valor, modelo, CancellationToken.None);

        Assert.Equal("Corolla", veiculo.Modelo.Descricao);
        Assert.Equal("Toyota", veiculo.Modelo.Marca);
        Assert.Equal(2022, veiculo.Modelo.Ano);
        repo.Verify(r => r.Salvar(It.IsAny<CancellationToken>()), Times.Once);
    }

}
