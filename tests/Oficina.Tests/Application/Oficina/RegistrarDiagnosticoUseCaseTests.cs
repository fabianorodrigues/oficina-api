using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;
using Oficina.Infrastructure.Email.Configurations;
using Oficina.Infrastructure.Email.Providers;
using Oficina.Infrastructure.Notificacoes;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class RegistrarDiagnosticoUseCaseTests
{
    [Fact]
    public async Task RegistrarDiagnostico_Deve_lancar_quando_os_nao_existe()
    {
        var repoOficina = new Mock<IOficinaRepository>();
        repoOficina.Setup(x => x.ObterOrdemServico(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdemServico?)null);

        var useCase = new RegistrarDiagnosticoUseCase(
            repoOficina.Object,
            Mock.Of<ICatalogoEstoqueRepository>(),
            Mock.Of<INotificadorCliente>());

        await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(Guid.NewGuid(), "x", [Guid.NewGuid()], CancellationToken.None));
    }

    [Fact]
    public async Task RegistrarDiagnostico_DeveGerarOrcamentoComTokenEEnviarEmail()
    {
        var repoOficina = new Mock<IOficinaRepository>();
        var repoCatalogo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();

        var os = OrdemServico.CriarCorretiva(Guid.NewGuid());
        var servico = new Servico(250);

        Orcamento? orcamentoAdicionado = null;

        repoOficina.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        repoCatalogo.Setup(x => x.ObterServico(servico.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servico);

        repoOficina.Setup(x => x.AdicionarOrcamento(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()))
            .Callback<Orcamento, CancellationToken>((o, _) => orcamentoAdicionado = o)
            .Returns(Task.CompletedTask);

        var useCase = new RegistrarDiagnosticoUseCase(repoOficina.Object, repoCatalogo.Object, notificador.Object);

        var resposta = await useCase.Executar(os.Id, "Falha no freio", [servico.Id], CancellationToken.None);

        Assert.NotEqual(Guid.Empty, resposta.OrcamentoId);
        Assert.Equal(resposta.OrcamentoId, os.OrcamentoId);
        Assert.NotNull(orcamentoAdicionado);
        Assert.False(string.IsNullOrWhiteSpace(orcamentoAdicionado!.TokenAcaoExterna));
        Assert.True(orcamentoAdicionado.TokenAcaoExternaExpiraEm > DateTimeOffset.UtcNow.AddDays(6));

        repoOficina.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Exactly(2));
        notificador.Verify(x => x.NotificarOrcamentoCriado(resposta.OrcamentoId, os.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarDiagnostico_QuandoEnvioDeEmailFalha_DeveManterFluxoPrincipal()
    {
        var repoOficina = new Mock<IOficinaRepository>();
        var repoCatalogo = new Mock<ICatalogoEstoqueRepository>();
        var repoCadastro = new Mock<ICadastroRepository>();

        var os = OrdemServico.CriarCorretiva(Guid.NewGuid());
        var servico = new Servico(250);
        Orcamento? orcamentoAdicionado = null;

        var cliente = new Cliente(
            new DocumentoCpfCnpj("52998224725"),
            "Joao Cliente",
            new Contato("cliente@teste.com", "11999999999"));
        var veiculo = new Veiculo(
            cliente.Id,
            new Placa("ABC1234"),
            new Renavam("12345678901"),
            new Modelo("Gol", "VW", 2020));

        repoOficina.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);
        repoOficina.Setup(x => x.ObterOrcamento(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => orcamentoAdicionado);
        repoOficina.Setup(x => x.AdicionarOrcamento(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()))
            .Callback<Orcamento, CancellationToken>((o, _) => orcamentoAdicionado = o)
            .Returns(Task.CompletedTask);

        repoCatalogo.Setup(x => x.ObterServico(servico.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servico);
        repoCadastro.Setup(x => x.ObterVeiculo(os.VeiculoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(veiculo);
        repoCadastro.Setup(x => x.ObterCliente(cliente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var emailOptions = Options.Create(new EmailSettings
        {
            BaseUrlAprovaRecusaOrcamento = "http://localhost:8080",
            From = "no-reply@oficina.local",
            SmtpHost = "localhost",
            SmtpPort = 25,
            Username = "usuario"
        });
        var emailSender = new MailKitEmailSender(emailOptions, Mock.Of<ILogger<MailKitEmailSender>>());
        var notificador = new NotificadorCliente(
            Mock.Of<ILogger<NotificadorCliente>>(),
            repoOficina.Object,
            repoCadastro.Object,
            emailSender,
            emailOptions);

        var useCase = new RegistrarDiagnosticoUseCase(repoOficina.Object, repoCatalogo.Object, notificador);

        var resposta = await useCase.Executar(os.Id, "Falha no freio", [servico.Id], CancellationToken.None);

        Assert.NotEqual(Guid.Empty, resposta.OrcamentoId);
        Assert.Equal(resposta.OrcamentoId, os.OrcamentoId);
        Assert.NotNull(orcamentoAdicionado);
        repoOficina.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RegistrarDiagnostico_DeveRetornarConflito_QuandoOsJaPossuiOrcamento()
    {
        var repoOficina = new Mock<IOficinaRepository>();
        var repoCatalogo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();

        var os = OrdemServico.CriarCorretiva(Guid.NewGuid());
        var orcamentoExistente = new Orcamento(os.Id, 250);

        repoOficina.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);
        repoOficina.Setup(x => x.ObterOrcamentoPorOs(os.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamentoExistente);

        var useCase = new RegistrarDiagnosticoUseCase(repoOficina.Object, repoCatalogo.Object, notificador.Object);

        var ex = await Assert.ThrowsAsync<OficinaException>(() =>
            useCase.Executar(os.Id, "Falha no freio", [Guid.NewGuid()], CancellationToken.None));

        Assert.Equal(409, ex.StatusHttp);
        repoOficina.Verify(x => x.AdicionarOrcamento(It.IsAny<Orcamento>(), It.IsAny<CancellationToken>()), Times.Never);
        notificador.Verify(x => x.NotificarOrcamentoCriado(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repoOficina.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Never);
    }
}
