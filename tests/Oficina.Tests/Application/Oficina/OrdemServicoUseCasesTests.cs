using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Oficina;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class OrdemServicoUseCasesTests
{
    [Fact]
    public async Task ObterOrdemServico_DeveRetornarOsSemOrcamentoQuandoNaoVinculado()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());

        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var useCase = new ObterOrdemServicoUseCase(repo.Object);

        var (ordem, orcamento) = await useCase.Executar(os.Id, CancellationToken.None);

        Assert.Equal(os.Id, ordem.Id);
        Assert.Null(orcamento);
    }

    [Fact]
    public async Task ObterOrdemServico_DeveFalharCom404_QuandoNaoEncontrada()
    {
        var repo = new Mock<IOficinaRepository>();
        repo.Setup(x => x.ObterOrdemServico(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdemServico?)null);

        var useCase = new ObterOrdemServicoUseCase(repo.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, ex.StatusHttp);
    }

    [Fact]
    public async Task FinalizarOrdemServico_DeveSalvarQuandoTransicaoValida()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = CriarOsEmExecucao();

        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var useCase = new FinalizarOrdemServicoUseCase(repo.Object);
        await useCase.Executar(os.Id, CancellationToken.None);

        Assert.Equal("Finalizada", os.Status.ToString());
        Assert.NotNull(os.DataFimExecucao);
        repo.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EntregarOrdemServico_DeveFalharQuandoStatusInvalido()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());

        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var useCase = new EntregarOrdemServicoUseCase(repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.Executar(os.Id, CancellationToken.None));

        repo.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EntregarOrdemServico_DeveSalvarQuandoFinalizada()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = CriarOsFinalizada();

        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var useCase = new EntregarOrdemServicoUseCase(repo.Object);
        await useCase.Executar(os.Id, CancellationToken.None);

        Assert.Equal("Entregue", os.Status.ToString());
        repo.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static OrdemServico CriarOsEmExecucao()
    {
        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(os.Id, 100);
        os.VincularOrcamento(orcamento.Id);
        orcamento.Aprovar();
        os.IniciarExecucao(orcamento);
        return os;
    }

    private static OrdemServico CriarOsFinalizada()
    {
        var os = CriarOsEmExecucao();
        os.Finalizar();
        return os;
    }
}
