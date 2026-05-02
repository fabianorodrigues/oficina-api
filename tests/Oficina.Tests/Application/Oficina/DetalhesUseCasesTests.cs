using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class DetalhesUseCasesTests
{
    [Fact]
    public async Task ObterOrcamentoDetalhado_DeveFalharQuandoOrcamentoNaoExiste()
    {
        var oficina = new Mock<IOficinaRepository>();
        var catalogo = new Mock<ICatalogoEstoqueRepository>();

        oficina.Setup(x => x.ObterOrcamento(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        var useCase = new ObterOrcamentoDetalhadoUseCase(oficina.Object, catalogo.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, ex.StatusHttp);
    }

    [Fact]
    public async Task ObterOrcamentoDetalhado_DevePermitirDescricaoNulaQuandoMaterialNaoEncontrado()
    {
        var oficina = new Mock<IOficinaRepository>();
        var catalogo = new Mock<ICatalogoEstoqueRepository>();

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var materialId = Guid.NewGuid();
        var orcamento = new Orcamento(os.Id, 30);
        orcamento.DefinirItensMaterial([new OrcamentoItemMaterial(TipoMaterial.Insumo, materialId, 1, 30)]);

        oficina.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orcamento);
        catalogo.Setup(x => x.ObterInsumo(materialId, It.IsAny<CancellationToken>())).ReturnsAsync((Insumo?)null);

        var useCase = new ObterOrcamentoDetalhadoUseCase(oficina.Object, catalogo.Object);
        var resposta = await useCase.Executar(orcamento.Id, CancellationToken.None);

        Assert.Single(resposta.ItensMaterial);
        Assert.Null(resposta.ItensMaterial[0].Descricao);
    }

    [Fact]
    public async Task ObterOrdemServicoDetalhada_DeveRetornarSemOrcamentoQuandoNaoVinculada()
    {
        var oficina = new Mock<IOficinaRepository>();
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());

        oficina.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);

        var obterOrcamento = new ObterOrcamentoDetalhadoUseCase(oficina.Object, Mock.Of<ICatalogoEstoqueRepository>());
        var useCase = new ObterOrdemServicoDetalhadaUseCase(oficina.Object, obterOrcamento);

        var resposta = await useCase.Executar(os.Id, CancellationToken.None);

        Assert.Equal(os.Id, resposta.Id);
        Assert.Null(resposta.Orcamento);
    }

    [Fact]
    public async Task ObterOrdemServicoDetalhada_DeveFalharCom404QuandoNaoEncontrada()
    {
        var oficina = new Mock<IOficinaRepository>();
        oficina.Setup(x => x.ObterOrdemServico(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdemServico?)null);

        var useCase = new ObterOrdemServicoDetalhadaUseCase(
            oficina.Object,
            new ObterOrcamentoDetalhadoUseCase(oficina.Object, Mock.Of<ICatalogoEstoqueRepository>()));

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, ex.StatusHttp);
    }
}
