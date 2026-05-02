using Moq;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Application;

public class AprovarOrcamentoUseCaseTests
{
    [Fact]
    public async Task Aprovar_OrcamentoAprovadoEIniciarExecucao()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var estoqueRepo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), new[] { Guid.NewGuid() });

        var materialId = Guid.NewGuid();
        var orc = new Orcamento(os.Id, 100);

        os.VincularOrcamento(orc.Id);

        oficinaRepo.Setup(x => x.ObterOrcamento(orc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orc);
        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var uc = new AprovarOrcamentoUseCase(oficinaRepo.Object, estoqueRepo.Object);

        await uc.Executar(orc.Id, CancellationToken.None);

        Assert.Equal(StatusOrcamento.Aprovado, orc.Status);
        Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
        Assert.Equal(OrigemAtualizacaoStatusOs.Interna, os.OrigemUltimaAtualizacaoStatus);

        oficinaRepo.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Once);        
    }
}
