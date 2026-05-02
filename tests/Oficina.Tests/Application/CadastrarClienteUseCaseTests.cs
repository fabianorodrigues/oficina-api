using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.UseCases.Cadastro;
using Oficina.Domain.Cadastro;
using Xunit;

namespace Oficina.Tests.Application;

public class CadastrarClienteUseCaseTests
{
    [Fact]
    public async Task DeveCadastrarCliente_QuandoNaoExistir()
    {
        var repo = new Mock<ICadastroRepository>();
        repo.Setup(x => x.ExisteClientePorDocumento(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var uc = new CadastrarClienteUseCase(repo.Object);

        var id = await uc.Executar("111.444.777-35", "Teste Nome", "email@email.com", "11942189715", CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        repo.Verify(x => x.AdicionarCliente(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Once);
    }
}
