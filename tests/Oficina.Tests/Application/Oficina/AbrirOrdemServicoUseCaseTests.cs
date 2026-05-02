using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class AbrirOrdemServicoUseCaseTests
{
    [Fact]
    public async Task Abrir_ComClienteVeiculoEServicos_DeveRetornarIdStatusTotal()
    {
        var fixture = CriarFixture();
        var servico = new Servico(100);

        fixture.Catalogo.Setup(x => x.ObterServico(servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servico);

        var req = CriarRequest(servico.Id);
        var result = await fixture.UseCase.Executar(req, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(StatusOrdemServico.Recebida.ToString(), result.Status);
        Assert.Equal(100m, result.Total);
    }

    [Fact]
    public async Task Abrir_ComClienteVeiculoServicosEPecas_DeveSomarPecasNoTotal()
    {
        var fixture = CriarFixture();
        var servico = new Servico(100);
        var peca = new Peca(50, "Filtro");

        fixture.Catalogo.Setup(x => x.ObterServico(servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servico);
        fixture.Catalogo.Setup(x => x.ObterPeca(peca.Id, It.IsAny<CancellationToken>())).ReturnsAsync(peca);

        var req = CriarRequest(servico.Id, pecas: [new PecaAberturaRequest { PecaId = peca.Id, Quantidade = 2 }]);
        var result = await fixture.UseCase.Executar(req, CancellationToken.None);

        Assert.Equal(200m, result.Total);
    }

    [Fact]
    public async Task Abrir_ComClienteVeiculoServicosPecasEInsumos_DeveSomarTudoNoTotal()
    {
        var fixture = CriarFixture();
        var servico = new Servico(100);
        var peca = new Peca(50, "Filtro");
        var insumo = new Insumo(25, "Aditivo");

        fixture.Catalogo.Setup(x => x.ObterServico(servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servico);
        fixture.Catalogo.Setup(x => x.ObterPeca(peca.Id, It.IsAny<CancellationToken>())).ReturnsAsync(peca);
        fixture.Catalogo.Setup(x => x.ObterInsumo(insumo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(insumo);

        var req = CriarRequest(
            servico.Id,
            pecas: [new PecaAberturaRequest { PecaId = peca.Id, Quantidade = 2 }],
            insumos: [new InsumoAberturaRequest { InsumoId = insumo.Id, Quantidade = 1 }]);

        var result = await fixture.UseCase.Executar(req, CancellationToken.None);

        Assert.Equal(225m, result.Total);
    }

    [Fact]
    public async Task Abrir_DeveReutilizarClienteEVeiculoExistentes()
    {
        var fixture = CriarFixture();
        var servico = new Servico(100);
        var cliente = new Cliente(new DocumentoCpfCnpj("12345678909"), "João", new Contato("joao@email.com", "11999999999"));
        var veiculo = new Veiculo(cliente.Id, new Placa("ABC1234"), new Renavam("12345678901"), new Modelo("Corolla", "Toyota", 2020));

        fixture.Cadastro.Setup(x => x.ObterClientePorDocumento(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        fixture.Cadastro.Setup(x => x.ObterVeiculoPorPlaca(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        fixture.Catalogo.Setup(x => x.ObterServico(servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servico);

        var req = CriarRequest(servico.Id);
        var result = await fixture.UseCase.Executar(req, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        fixture.Cadastro.Verify(x => x.AdicionarCliente(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Cadastro.Verify(x => x.AdicionarVeiculo(It.IsAny<Veiculo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AbrirOrdemServicoRequest CriarRequest(
        Guid servicoId,
        IReadOnlyList<PecaAberturaRequest>? pecas = null,
        IReadOnlyList<InsumoAberturaRequest>? insumos = null)
        => new()
        {
            Cliente = new ClienteAberturaRequest
            {
                Nome = "João da Silva",
                Documento = "12345678909",
                Email = "joao@email.com",
                Telefone = "11999999999"
            },
            Veiculo = new VeiculoAberturaRequest
            {
                Placa = "ABC1234",
                Renavam = "12345678901",
                Modelo = new ModeloAberturaRequest
                {
                    Descricao = "Corolla",
                    Marca = "Toyota",
                    Ano = 2020
                }
            },
            Itens = new ItensAberturaRequest
            {
                Servicos = [new ServicoAberturaRequest { ServicoId = servicoId }],
                Pecas = pecas ?? [],
                Insumos = insumos ?? []
            }
        };

    private static Fixture CriarFixture()
    {
        var cadastro = new Mock<ICadastroRepository>();
        var catalogo = new Mock<ICatalogoEstoqueRepository>();
        var oficina = new Mock<IOficinaRepository>();

        cadastro.Setup(x => x.ObterClientePorDocumento(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);
        cadastro.Setup(x => x.ObterVeiculoPorPlaca(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Veiculo?)null);

        var uc = new AbrirOrdemServicoUseCase(cadastro.Object, catalogo.Object, oficina.Object);
        return new Fixture(uc, cadastro, catalogo, oficina);
    }

    private sealed record Fixture(
        AbrirOrdemServicoUseCase UseCase,
        Mock<ICadastroRepository> Cadastro,
        Mock<ICatalogoEstoqueRepository> Catalogo,
        Mock<IOficinaRepository> Oficina);
}
