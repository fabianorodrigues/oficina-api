using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.UseCases.CatalogoEstoque;
using Oficina.Domain.CatalogoEstoque;
using Xunit;

namespace Oficina.Tests.Application.CatalogoEstoque;

public class EstoqueUseCasesTests
{
    [Fact]
    public async Task RegistrarEntradaInsumo_deve_atualizar_quando_existir()
    {
        var repo = new Mock<ICatalogoEstoqueRepository>();
        var insumoId = Guid.NewGuid();
        var estoque = new EstoqueInsumo(insumoId, 1);

        repo.Setup(r => r.ObterEstoqueInsumo(insumoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estoque);

        var uc = new AjustarEstoqueInsumoUseCase(repo.Object);
        await uc.Executar(insumoId, 4, CancellationToken.None);

        Assert.Equal(5, estoque.Quantidade);
        repo.Verify(r => r.Salvar(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.AdicionarEstoqueInsumo(It.IsAny<EstoqueInsumo>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
