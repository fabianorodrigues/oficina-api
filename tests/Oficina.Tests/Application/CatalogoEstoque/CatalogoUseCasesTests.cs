using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.UseCases.CatalogoEstoque;
using Oficina.Domain.CatalogoEstoque;
using Xunit;

namespace Oficina.Tests.Application.CatalogoEstoque;

public class CatalogoUseCasesTests
{
    [Fact]
    public async Task CadastrarServico_deve_adicionar_e_salvar()
    {
        var repo = new Mock<ICatalogoEstoqueRepository>();
        var uc = new CadastrarServicoUseCase(repo.Object);

        var id = await uc.Executar(150m, Enumerable.Empty<(Guid, int)> (), Enumerable.Empty<(Guid, int)>(), CancellationToken.None);

        repo.Verify(r => r.AdicionarServico(It.IsAny<Servico>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.Salvar(It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task AtualizarServico_DeveSubstituirPecasEInsumos()
    {
        var servico = new Servico(150m);
        var pecaOriginal = Guid.NewGuid();
        var insumoOriginal = Guid.NewGuid();
        var pecaNova = Guid.NewGuid();
        var insumoNovo = Guid.NewGuid();
        servico.AdicionarPeca(pecaOriginal, 1);
        servico.AdicionarInsumo(insumoOriginal, 2);

        var repo = new Mock<ICatalogoEstoqueRepository>();
        repo.Setup(x => x.ObterServico(servico.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servico);
        var uc = new AtualizarServicoUseCase(repo.Object);

        await uc.Executar(275m, servico.Id, [(pecaNova, 3)], [(insumoNovo, 4)], CancellationToken.None);

        Assert.Equal(275m, servico.MaoDeObra);
        var peca = Assert.Single(servico.Pecas);
        Assert.Equal(pecaNova, peca.PecaId);
        Assert.Equal(3, peca.Quantidade);
        var insumo = Assert.Single(servico.Insumos);
        Assert.Equal(insumoNovo, insumo.InsumoId);
        Assert.Equal(4, insumo.Quantidade);
        repo.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CadastrarPeca_deve_adicionar_e_salvar()
    {
        var repo = new Mock<ICatalogoEstoqueRepository>();
        var uc = new CadastrarPecaUseCase(repo.Object);

        var id = await uc.Executar(50m, "Filtro original", CancellationToken.None);

        repo.Verify(r => r.AdicionarPeca(It.IsAny<Peca>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.Salvar(It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task CadastrarInsumo_deve_adicionar_e_salvar()
    {
        var repo = new Mock<ICatalogoEstoqueRepository>();
        var uc = new CadastrarInsumoUseCase(repo.Object);

        var id = await uc.Executar(35m, "Óleo sintético", CancellationToken.None);

        repo.Verify(r => r.AdicionarInsumo(It.IsAny<Insumo>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.Salvar(It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotEqual(Guid.Empty, id);
    }
}
