using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Application.DTO.Seguranca;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Seguranca;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Domain.Oficina;
using Oficina.Domain.Seguranca;
using Oficina.Domain.Seguranca.Enums;
using Xunit;

namespace Oficina.Tests.Application.Seguranca;

public class SegurancaUseCasesTests
{
    [Fact]
    public async Task AutenticarCliente_ComCpfCadastrado_DeveRetornarCliente()
    {
        var cliente = new Cliente(new DocumentoCpfCnpj("39053344705"), "Cliente", new Contato("c@teste.com", "11999999999"));
        var repo = new Mock<ICadastroRepository>();
        repo.Setup(x => x.ObterClientePorDocumento("39053344705", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var useCase = new AutenticarClienteUseCase(repo.Object);

        var resultado = await useCase.Executar("390.533.447-05", CancellationToken.None);

        Assert.Equal(cliente.Id, resultado.Id);
    }

    [Fact]
    public async Task AutenticarCliente_ComCpfInexistente_DeveFalhar()
    {
        var repo = new Mock<ICadastroRepository>();
        repo.Setup(x => x.ObterClientePorDocumento("39053344705", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);

        var useCase = new AutenticarClienteUseCase(repo.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() => useCase.Executar("39053344705", CancellationToken.None));
        Assert.Equal(401, ex.StatusHttp);
    }

    [Fact]
    public async Task AutenticarFuncionario_ComSenhaValida_DeveRetornarFuncionario()
    {
        var funcionario = new Funcionario("Maria", "39053344705", "hash", PerfilUsuarioInterno.Funcionario);
        var repo = new Mock<IFuncionarioRepository>();
        var password = new Mock<IPasswordHashService>();
        repo.Setup(x => x.ObterPorCpf("39053344705", It.IsAny<CancellationToken>())).ReturnsAsync(funcionario);
        password.Setup(x => x.Verificar("hash", "Senha@123")).Returns(true);

        var useCase = new AutenticarFuncionarioUseCase(repo.Object, password.Object);

        var resultado = await useCase.Executar("39053344705", "Senha@123", CancellationToken.None);

        Assert.Equal(funcionario.Id, resultado.Id);
    }

    [Fact]
    public async Task AutenticarFuncionario_ComSenhaInvalida_DeveFalhar()
    {
        var funcionario = new Funcionario("Maria", "39053344705", "hash", PerfilUsuarioInterno.Funcionario);
        var repo = new Mock<IFuncionarioRepository>();
        var password = new Mock<IPasswordHashService>();
        repo.Setup(x => x.ObterPorCpf("39053344705", It.IsAny<CancellationToken>())).ReturnsAsync(funcionario);
        password.Setup(x => x.Verificar("hash", "errada")).Returns(false);

        var useCase = new AutenticarFuncionarioUseCase(repo.Object, password.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() => useCase.Executar("39053344705", "errada", CancellationToken.None));
        Assert.Equal(401, ex.StatusHttp);
    }

    [Fact]
    public async Task AutenticarFuncionario_Inativo_DeveFalhar()
    {
        var funcionario = new Funcionario("Maria", "39053344705", "hash", PerfilUsuarioInterno.Funcionario);
        funcionario.Inativar();
        var repo = new Mock<IFuncionarioRepository>();
        var password = new Mock<IPasswordHashService>();
        repo.Setup(x => x.ObterPorCpf("39053344705", It.IsAny<CancellationToken>())).ReturnsAsync(funcionario);
        password.Setup(x => x.Verificar("hash", "Senha@123")).Returns(true);

        var useCase = new AutenticarFuncionarioUseCase(repo.Object, password.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() => useCase.Executar("39053344705", "Senha@123", CancellationToken.None));
        Assert.Equal(401, ex.StatusHttp);
    }

    [Theory]
    [InlineData("Funcionario")]
    [InlineData("Admin")]
    public async Task CriarFuncionario_DevePermitirPerfisInternosESemExporSenhaHash(string perfil)
    {
        var repo = new Mock<IFuncionarioRepository>();
        var password = new Mock<IPasswordHashService>();
        password.Setup(x => x.Hash("Senha@123")).Returns("hash");
        repo.Setup(x => x.ExistePorCpf("39053344705", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var useCase = new CriarFuncionarioUseCase(repo.Object, password.Object);

        var response = await useCase.Executar(
            new CriarFuncionarioRequest("Maria Oficina", "39053344705", "Senha@123", perfil),
            CancellationToken.None);

        Assert.Equal(perfil, response.Perfil);
        Assert.DoesNotContain(response.GetType().GetProperties(), p => p.Name == "SenhaHash");
        repo.Verify(x => x.Adicionar(It.Is<Funcionario>(f => f.SenhaHash == "hash"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ownership_DevePermitirOrdemDoCliente()
    {
        var clienteId = Guid.NewGuid();
        var veiculo = new Veiculo(clienteId, new Placa("ABC1D23"), new Renavam("12345678901"), new Modelo("Uno", "Fiat", 2020));
        var os = OrdemServico.CriarCorretiva(veiculo.Id);
        var cadastro = new Mock<ICadastroRepository>();
        var oficina = new Mock<IOficinaRepository>();
        oficina.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        cadastro.Setup(x => x.ObterVeiculo(veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        var ownership = new ClienteOwnershipService(cadastro.Object, oficina.Object);

        await ownership.GarantirOrdemServico(clienteId, os.Id, CancellationToken.None);
    }

    [Fact]
    public async Task Ownership_DeveBloquearOrdemDeOutroCliente()
    {
        var veiculo = new Veiculo(Guid.NewGuid(), new Placa("ABC1D23"), new Renavam("12345678901"), new Modelo("Uno", "Fiat", 2020));
        var os = OrdemServico.CriarCorretiva(veiculo.Id);
        var cadastro = new Mock<ICadastroRepository>();
        var oficina = new Mock<IOficinaRepository>();
        oficina.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        cadastro.Setup(x => x.ObterVeiculo(veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        var ownership = new ClienteOwnershipService(cadastro.Object, oficina.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() => ownership.GarantirOrdemServico(Guid.NewGuid(), os.Id, CancellationToken.None));
        Assert.Equal(403, ex.StatusHttp);
    }
}
