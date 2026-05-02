using Moq;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class OrcamentoUseCasesTests
{
    [Fact]
    public async Task RecusarOrcamento_DeveFinalizarOsESolicitarRetirada()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var catalogo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();

        var os = OrdemServico.CriarCorretiva(Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>( () =>
           Task.Run(() => os.RegistrarDiagnostico("Barulho na suspensão", Enumerable.Empty<Guid>())));

        var orc = new Orcamento(os.Id, 50);

        oficinaRepo.Setup(x => x.ObterOrcamento(orc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orc);
        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var uc = new RecusarOrcamentoUseCase(oficinaRepo.Object, notificador.Object);

        await uc.Executar(orc.Id, CancellationToken.None);

        Assert.Equal(StatusOrcamento.Recusado, orc.Status);
        Assert.Equal(StatusOrdemServico.Finalizada, os.Status);
        Assert.Equal(OrigemAtualizacaoStatusOs.Interna, os.OrigemUltimaAtualizacaoStatus);

        oficinaRepo.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Once);
        notificador.Verify(x => x.NotificarOrcamentoRecusado(orc.Id, os.Id, It.IsAny<CancellationToken>()), Times.Once);
        catalogo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ObterOrcamento_DeveRetornarDto()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var orc = new Orcamento(Guid.NewGuid(), 120);

        oficinaRepo.Setup(x => x.ObterOrcamento(orc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orc);
        var uc = new ObterOrcamentoUseCase(oficinaRepo.Object);

        var dto = await uc.Executar(orc.Id, CancellationToken.None);

        Assert.Equal(orc.Id, dto.Id);
        Assert.Equal(orc.ValorTotal, dto.ValorTotal);
        Assert.Equal(orc.Status, dto.Status);
    }


    [Fact]
    public async Task ObterOrcamentoDetalhado_DeveRetornarDescricaoDosMateriais()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var catalogoRepo = new Mock<ICatalogoEstoqueRepository>();

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var peca = new Peca(120, "Filtro de óleo");
        var insumo = new Insumo(30, "Aditivo radiador");

        var orc = new Orcamento(os.Id, 180);
        orc.DefinirItensMaterial(
        [
            new OrcamentoItemMaterial(TipoMaterial.Peca, peca.Id, 1, 120),
            new OrcamentoItemMaterial(TipoMaterial.Insumo, insumo.Id, 2, 30)
        ]);

        oficinaRepo.Setup(x => x.ObterOrcamento(orc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orc);
        catalogoRepo.Setup(x => x.ObterPeca(peca.Id, It.IsAny<CancellationToken>())).ReturnsAsync(peca);
        catalogoRepo.Setup(x => x.ObterInsumo(insumo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(insumo);

        var uc = new ObterOrcamentoDetalhadoUseCase(oficinaRepo.Object, catalogoRepo.Object);

        var detalhe = await uc.Executar(orc.Id, CancellationToken.None);

        Assert.Equal(2, detalhe.ItensMaterial.Count);
        Assert.Contains(detalhe.ItensMaterial, x => x.MaterialId == peca.Id && x.Descricao == "Filtro de óleo");
        Assert.Contains(detalhe.ItensMaterial, x => x.MaterialId == insumo.Id && x.Descricao == "Aditivo radiador");
    }

    [Fact]
    public async Task ObterOrdemServicoDetalhada_DeveRetornarOrcamentoQuandoVinculado()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var catalogoRepo = new Mock<ICatalogoEstoqueRepository>();

        var os = OrdemServico.CriarCorretiva(Guid.NewGuid());
        os.RegistrarDiagnostico("Falha na ignição", [Guid.NewGuid()]);

        var orc = new Orcamento(os.Id, 450);
        os.VincularOrcamento(orc.Id);

        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        oficinaRepo.Setup(x => x.ObterOrcamento(orc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orc);

        var obterOrcamento = new ObterOrcamentoDetalhadoUseCase(oficinaRepo.Object, catalogoRepo.Object);
        var uc = new ObterOrdemServicoDetalhadaUseCase(oficinaRepo.Object, obterOrcamento);

        var detalhe = await uc.Executar(os.Id, CancellationToken.None);

        Assert.NotNull(detalhe.Orcamento);
        Assert.Equal(orc.Id, detalhe.Orcamento!.Id);
        Assert.Equal(os.Id, detalhe.Id);
    }

}
