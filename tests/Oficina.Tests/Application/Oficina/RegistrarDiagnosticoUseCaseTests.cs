using Moq;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class RegistrarDiagnosticoUseCaseTests
{
    [Fact]
    public async Task RegistrarDiagnostico_Deve_lancar_quando_os_nao_existe()
    {
        var repoOficina = new Mock<IOficinaRepository>();
        repoOficina.Setup(x => x.ObterOrdemServico(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdemServico?)null);

        var useCase = new RegistrarDiagnosticoUseCase(
            repoOficina.Object,
            Mock.Of<ICatalogoEstoqueRepository>(),
            Mock.Of<INotificadorCliente>());

        await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), "x", [Guid.NewGuid()], CancellationToken.None));
    }

    [Fact]
    public async Task RegistrarDiagnostico_DeveGerarOrcamentoComTokenEEnviarEmail()
    {
        var repoOficina = new Mock<IOficinaRepository>();
        var repoCatalogo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();

        var os = OrdemServico.CriarCorretiva(Guid.NewGuid());
        var servico = new Servico(250);

        Orcamento? orcamentoAdicionado = null;

        repoOficina.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        repoCatalogo.Setup(x => x.ObterServico(servico.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servico);

        repoOficina.Setup(x => x.AdicionarOrcamento(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()))
            .Callback<Orcamento, CancellationToken>((o, _) => orcamentoAdicionado = o)
            .Returns(Task.CompletedTask);

        var useCase = new RegistrarDiagnosticoUseCase(repoOficina.Object, repoCatalogo.Object, notificador.Object);

        var resposta = await useCase.Executar(os.Id, "Falha no freio", [servico.Id], CancellationToken.None);

        Assert.NotEqual(Guid.Empty, resposta.OrcamentoId);
        Assert.Equal(resposta.OrcamentoId, os.OrcamentoId);
        Assert.NotNull(orcamentoAdicionado);
        Assert.False(string.IsNullOrWhiteSpace(orcamentoAdicionado!.TokenAcaoExterna));
        Assert.True(orcamentoAdicionado.TokenAcaoExternaExpiraEm > DateTimeOffset.UtcNow.AddDays(6));

        repoOficina.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Exactly(2));
        notificador.Verify(x => x.NotificarOrcamentoCriado(resposta.OrcamentoId, os.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
