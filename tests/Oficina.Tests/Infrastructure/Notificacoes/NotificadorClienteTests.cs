using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Oficina.Application.Abstractions.Email;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Domain.Oficina;
using Oficina.Infrastructure.Email.Configurations;
using Oficina.Infrastructure.Email.Providers;
using Oficina.Infrastructure.Notificacoes;
using Xunit;

namespace Oficina.Tests.Infrastructure.Notificacoes;

public class NotificadorClienteTests
{
    [Fact]
    public async Task NotificarOrcamentoCriado_DeveEnviarEmailComLinksDeAprovarERecusar()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var cadastroRepo = new Mock<ICadastroRepository>();
        var emailSender = new Mock<IEmailSender>();
        var options = Options.Create(new EmailSettings
        {
            BaseUrlSmtp = "http://localhost:5000",
            BaseUrlAprovaRecusaOrcamento = "http://localhost:8080",
            From = "no-reply@oficina.local",
            SmtpHost = "smtp4dev",
            SmtpPort = 25
        });

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(os.Id, 500);
        orcamento.DefinirTokenAcaoExterna("TOKEN_EMAIL", DateTimeOffset.UtcNow.AddDays(1));
        os.VincularOrcamento(orcamento.Id);

        var cliente = new Cliente(
            new DocumentoCpfCnpj("52998224725"),
            "João Cliente",
            new Contato("cliente@teste.com", "11999999999"));
        var veiculo = new Veiculo(
            cliente.Id,
            new Placa("ABC1234"),
            new Renavam("12345678901"),
            new Modelo("Gol", "VW", 2020));

        oficinaRepo.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        cadastroRepo.Setup(x => x.ObterVeiculo(os.VeiculoId, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        cadastroRepo.Setup(x => x.ObterCliente(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);

        var notificador = new NotificadorCliente(
            Mock.Of<ILogger<NotificadorCliente>>(),
            oficinaRepo.Object,
            cadastroRepo.Object,
            emailSender.Object,
            options);

        await notificador.NotificarOrcamentoCriado(orcamento.Id, os.Id, CancellationToken.None);

        emailSender.Verify(x => x.Enviar(
            It.Is<EmailMessage>(m =>
                m.To == "cliente@teste.com" &&
                m.Subject.Contains("Orcamento") &&
                m.HtmlBody.Contains("http://localhost:8080/api/orcamentos/acoes-externas/aprovar?token=TOKEN_EMAIL") &&
                m.HtmlBody.Contains("http://localhost:8080/api/orcamentos/acoes-externas/recusar?token=TOKEN_EMAIL")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarOrcamentoCriado_DeveEscaparTokenNosLinks()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var cadastroRepo = new Mock<ICadastroRepository>();
        var emailSender = new Mock<IEmailSender>();
        var options = Options.Create(new EmailSettings
        {
            BaseUrlSmtp = "http://localhost:5000",
            BaseUrlAprovaRecusaOrcamento = "http://localhost:8080",
            From = "no-reply@oficina.local",
            SmtpHost = "smtp4dev",
            SmtpPort = 25
        });

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(os.Id, 500);
        orcamento.DefinirTokenAcaoExterna("TOKEN COM ESPAÇO", DateTimeOffset.UtcNow.AddDays(1));
        os.VincularOrcamento(orcamento.Id);

        var cliente = new Cliente(
            new DocumentoCpfCnpj("52998224725"),
            "João Cliente",
            new Contato("cliente@teste.com", "11999999999"));
        var veiculo = new Veiculo(
            cliente.Id,
            new Placa("ABC1234"),
            new Renavam("12345678901"),
            new Modelo("Gol", "VW", 2020));

        oficinaRepo.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        cadastroRepo.Setup(x => x.ObterVeiculo(os.VeiculoId, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        cadastroRepo.Setup(x => x.ObterCliente(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);

        var notificador = new NotificadorCliente(
            Mock.Of<ILogger<NotificadorCliente>>(),
            oficinaRepo.Object,
            cadastroRepo.Object,
            emailSender.Object,
            options);

        await notificador.NotificarOrcamentoCriado(orcamento.Id, os.Id, CancellationToken.None);

        emailSender.Verify(x => x.Enviar(
            It.Is<EmailMessage>(m =>
                m.HtmlBody.Contains("/aprovar?token=TOKEN%20COM%20ESPA%C3%87O") &&
                m.HtmlBody.Contains("/recusar?token=TOKEN%20COM%20ESPA%C3%87O")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarOrcamentoCriado_QuandoEmailFalha_NaoDevePropagarErro()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var cadastroRepo = new Mock<ICadastroRepository>();
        var emailSender = new Mock<IEmailSender>();
        var options = Options.Create(new EmailSettings
        {
            BaseUrlAprovaRecusaOrcamento = "http://localhost:8080",
            From = "no-reply@oficina.local",
            SmtpHost = "smtp4dev",
            SmtpPort = 25
        });

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(os.Id, 500);
        orcamento.DefinirTokenAcaoExterna("TOKEN_EMAIL", DateTimeOffset.UtcNow.AddDays(1));
        os.VincularOrcamento(orcamento.Id);

        var cliente = new Cliente(
            new DocumentoCpfCnpj("52998224725"),
            "Joao Cliente",
            new Contato("cliente@teste.com", "11999999999"));
        var veiculo = new Veiculo(
            cliente.Id,
            new Placa("ABC1234"),
            new Renavam("12345678901"),
            new Modelo("Gol", "VW", 2020));

        oficinaRepo.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        cadastroRepo.Setup(x => x.ObterVeiculo(os.VeiculoId, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        cadastroRepo.Setup(x => x.ObterCliente(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        emailSender
            .Setup(x => x.Enviar(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP indisponivel"));

        var notificador = new NotificadorCliente(
            Mock.Of<ILogger<NotificadorCliente>>(),
            oficinaRepo.Object,
            cadastroRepo.Object,
            emailSender.Object,
            options);

        await notificador.NotificarOrcamentoCriado(orcamento.Id, os.Id, CancellationToken.None);

        emailSender.Verify(x => x.Enviar(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarOrcamentoCriado_QuandoSmtpEstiverParcial_NaoDevePropagarErro()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var cadastroRepo = new Mock<ICadastroRepository>();
        var options = Options.Create(new EmailSettings
        {
            BaseUrlAprovaRecusaOrcamento = "http://localhost:8080",
            From = "no-reply@oficina.local",
            SmtpHost = "localhost",
            SmtpPort = 25,
            Username = "usuario"
        });
        var emailSender = new MailKitEmailSender(options, Mock.Of<ILogger<MailKitEmailSender>>());

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(os.Id, 500);
        orcamento.DefinirTokenAcaoExterna("TOKEN_EMAIL", DateTimeOffset.UtcNow.AddDays(1));
        os.VincularOrcamento(orcamento.Id);

        var cliente = new Cliente(
            new DocumentoCpfCnpj("52998224725"),
            "Joao Cliente",
            new Contato("cliente@teste.com", "11999999999"));
        var veiculo = new Veiculo(
            cliente.Id,
            new Placa("ABC1234"),
            new Renavam("12345678901"),
            new Modelo("Gol", "VW", 2020));

        oficinaRepo.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        cadastroRepo.Setup(x => x.ObterVeiculo(os.VeiculoId, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        cadastroRepo.Setup(x => x.ObterCliente(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);

        var notificador = new NotificadorCliente(
            Mock.Of<ILogger<NotificadorCliente>>(),
            oficinaRepo.Object,
            cadastroRepo.Object,
            emailSender,
            options);

        await notificador.NotificarOrcamentoCriado(orcamento.Id, os.Id, CancellationToken.None);
    }
}
