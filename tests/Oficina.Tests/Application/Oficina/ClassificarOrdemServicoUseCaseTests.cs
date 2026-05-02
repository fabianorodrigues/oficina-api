using Moq;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class ClassificarOrdemServicoUseCaseTests
{
    [Fact]
    public async Task ClassificarComoPreventiva_DeveAtualizarStatusParaAguardandoAprovacao()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());
        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var notificador = new Mock<INotificadorCliente>();

        var useCase = new ClassificarOrdemServicoUseCase(repo.Object, notificador.Object);
        await useCase.Executar(os.Id, "Preventiva", CancellationToken.None);

        Assert.Equal(TipoManutencao.Preventiva, os.TipoManutencao);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public async Task ClassificarComoCorretiva_DeveAtualizarStatusParaEmDiagnostico()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());
        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var notificador = new Mock<INotificadorCliente>();
        var useCase = new ClassificarOrdemServicoUseCase(repo.Object, notificador.Object);
        await useCase.Executar(os.Id, "Corretiva", CancellationToken.None);

        Assert.Equal(TipoManutencao.Corretiva, os.TipoManutencao);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
    }

    [Fact]
    public async Task ClassificarOsJaClassificada_DeveFalhar()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());
        os.Classificar(TipoManutencao.Corretiva);

        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var notificador = new Mock<INotificadorCliente>();
        var useCase = new ClassificarOrdemServicoUseCase(repo.Object, notificador.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.Executar(os.Id, "Preventiva", CancellationToken.None));
    }


    [Fact]
    public async Task ClassificarComTipoInvalido_DeveFalharCom400()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());
        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var notificador = new Mock<INotificadorCliente>();
        var useCase = new ClassificarOrdemServicoUseCase(repo.Object, notificador.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(os.Id, "TipoInexistente", CancellationToken.None));

        Assert.Equal(400, ex.StatusHttp);
    }
    [Fact]
    public async Task ClassificarOsInexistente_DeveFalharCom404()
    {
        var repo = new Mock<IOficinaRepository>();
        repo.Setup(x => x.ObterOrdemServico(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((OrdemServico?)null);

        var notificador = new Mock<INotificadorCliente>();
        var useCase = new ClassificarOrdemServicoUseCase(repo.Object, notificador.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), "Corretiva", CancellationToken.None));

        Assert.Equal(404, ex.StatusHttp);
    }
}
