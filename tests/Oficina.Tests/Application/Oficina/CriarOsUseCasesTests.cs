using Moq;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class CriarOsUseCasesTests
{
    [Fact]
    public async Task CriarOsPreventiva_Deve_lancar_quando_veiculo_nao_existe()
    {
        var repoCadastro = new Mock<ICadastroRepository>();
        repoCadastro.Setup(x => x.ObterVeiculo(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Veiculo?)null);

        var useCase = new CriarOsPreventivaUseCase(
            repoCadastro.Object,
            Mock.Of<ICatalogoEstoqueRepository>(),
            Mock.Of<IOficinaRepository>(),
            Mock.Of<INotificadorCliente>());

        await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), [Guid.NewGuid()], CancellationToken.None));
    }

    [Fact]
    public async Task CriarOsPreventiva_DeveGerarOrcamentoComTokenEEnviarEmail()
    {
        var repoCadastro = new Mock<ICadastroRepository>();
        var repoCatalogo = new Mock<ICatalogoEstoqueRepository>();
        var repoOficina = new Mock<IOficinaRepository>();
        var notificador = new Mock<INotificadorCliente>();

        var veiculo = new Veiculo(
            Guid.NewGuid(),
            new Placa("ABC1234"),
            new Renavam("12345678901"),
            new Modelo("Onix", "Chevrolet", 2022));

        var servico = new Servico(100);
        var peca = new Peca(80, "Filtro de óleo");
        var insumo = new Insumo(20, "Aditivo");
        servico.AdicionarPeca(peca.Id, 1);
        servico.AdicionarInsumo(insumo.Id, 2);

        Orcamento? orcamentoAdicionado = null;

        repoCadastro.Setup(x => x.ObterVeiculo(veiculo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(veiculo);

        repoCatalogo.Setup(x => x.ObterServico(servico.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servico);
        repoCatalogo.Setup(x => x.ObterPeca(peca.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(peca);
        repoCatalogo.Setup(x => x.ObterInsumo(insumo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(insumo);

        repoOficina.Setup(x => x.AdicionarOrcamento(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()))
            .Callback<Orcamento, CancellationToken>((o, _) => orcamentoAdicionado = o)
            .Returns(Task.CompletedTask);

        var useCase = new CriarOsPreventivaUseCase(
            repoCadastro.Object,
            repoCatalogo.Object,
            repoOficina.Object,
            notificador.Object);

        var resposta = await useCase.Executar(veiculo.Id, [servico.Id], CancellationToken.None);

        Assert.NotEqual(Guid.Empty, resposta.Id);
        Assert.NotEqual(Guid.Empty, resposta.OrcamentoId);
        Assert.NotNull(orcamentoAdicionado);
        Assert.False(string.IsNullOrWhiteSpace(orcamentoAdicionado!.TokenAcaoExterna));
        Assert.True(orcamentoAdicionado.TokenAcaoExternaExpiraEm > DateTimeOffset.UtcNow.AddDays(6));

        repoOficina.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Once);
        notificador.Verify(x => x.NotificarOrcamentoCriado(resposta.OrcamentoId, resposta.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
