using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Cadastro;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Xunit;

namespace Oficina.Tests.Application.Cadastro;

public class ConsultasUseCasesTests
{
    [Fact]
    public async Task ObterCliente_DeveRetornarQuandoExiste()
    {
        var repo = new Mock<ICadastroRepository>();
        var cliente = new Cliente(new DocumentoCpfCnpj("12345678909"), "Maria", new Contato("maria@email.com", "11999999999"));

        repo.Setup(x => x.ObterCliente(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);

        var useCase = new ObterClienteUseCase(repo.Object);
        var resultado = await useCase.Executar(cliente.Id, CancellationToken.None);

        Assert.Equal(cliente.Id, resultado.Id);
    }

    [Fact]
    public async Task ObterCliente_DeveFalharCom404QuandoNaoExiste()
    {
        var repo = new Mock<ICadastroRepository>();
        repo.Setup(x => x.ObterCliente(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Cliente?)null);

        var useCase = new ObterClienteUseCase(repo.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, ex.StatusHttp);
    }

    [Fact]
    public async Task ObterVeiculo_DeveFalharCom404QuandoNaoExiste()
    {
        var repo = new Mock<ICadastroRepository>();
        repo.Setup(x => x.ObterVeiculo(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Veiculo?)null);

        var useCase = new ObterVeiculoUseCase(repo.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, ex.StatusHttp);
    }

    [Fact]
    public async Task ListarVeiculosPorCliente_DeveRetornarColecaoRepositorio()
    {
        var repo = new Mock<ICadastroRepository>();
        var clienteId = Guid.NewGuid();
        var veiculo = new Veiculo(clienteId, new Placa("ABC1D23"), new Renavam("12345678901"), new Modelo("Onix", "Chevrolet", 2022));

        repo.Setup(x => x.ListarVeiculosPorCliente(clienteId, It.IsAny<CancellationToken>())).ReturnsAsync([veiculo]);

        var useCase = new ListarVeiculosPorClienteUseCase(repo.Object);
        var lista = await useCase.Executar(clienteId, CancellationToken.None);

        Assert.Single(lista);
        Assert.Equal(veiculo.Id, lista[0].Id);
    }
}
