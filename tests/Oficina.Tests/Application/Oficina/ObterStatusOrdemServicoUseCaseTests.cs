using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Oficina;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class ObterStatusOrdemServicoUseCaseTests
{
    [Fact]
    public async Task ObterStatus_DeveRetornarDadosQuandoOsExiste()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());

        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        var useCase = new ObterStatusOrdemServicoUseCase(repo.Object);

        var result = await useCase.Executar(os.Id, CancellationToken.None);

        Assert.Equal(os.Id, result.OrdemServicoId);
        Assert.Equal(os.Status.ToString(), result.Status);
        Assert.Equal(os.OrigemUltimaAtualizacaoStatus.ToString(), result.OrigemUltimaAtualizacaoStatus);
    }

    [Fact]
    public async Task ObterStatus_DeveRetornar404QuandoOsNaoExiste()
    {
        var repo = new Mock<IOficinaRepository>();
        repo.Setup(x => x.ObterOrdemServico(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((OrdemServico?)null);

        var useCase = new ObterStatusOrdemServicoUseCase(repo.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, ex.StatusHttp);
    }
}
