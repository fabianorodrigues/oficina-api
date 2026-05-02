using Microsoft.AspNetCore.Mvc;
using Moq;
using Oficina.Application.DTO.Oficina;
using Oficina.Api.Controllers;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Oficina;
using Xunit;

namespace Oficina.Tests.Api.Controllers;

public class OrdensServicoControllerTests
{
    [Fact]
    public async Task Listar_DeveRetornarOkComRespostaMontadaNaApplication()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var osRecebida = OrdemServico.CriarRecebida(Guid.NewGuid());

        oficinaRepo.Setup(x => x.ListarOrdensServico(It.IsAny<CancellationToken>()))
            .ReturnsAsync([osRecebida]);

        var controller = CriarController(oficinaRepo);

        var result = await controller.Listar(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<OrdemServicoListaItemResponse>>(ok.Value);
        Assert.Single(payload);
        Assert.Equal(osRecebida.Id, payload[0].Id);
        Assert.Equal(osRecebida.Status.ToString(), payload[0].Status);
    }

    private static OrdensServicoController CriarController(Mock<IOficinaRepository> oficinaRepo)
    {
        var cadastroRepo = new Mock<ICadastroRepository>();
        var catalogoRepo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();

        return new OrdensServicoController(
            new AbrirOrdemServicoUseCase(cadastroRepo.Object, catalogoRepo.Object, oficinaRepo.Object),
            new RegistrarDiagnosticoUseCase(oficinaRepo.Object, catalogoRepo.Object, notificador.Object),
            new ClassificarOrdemServicoUseCase(oficinaRepo.Object, notificador.Object),
            new ObterStatusOrdemServicoUseCase(oficinaRepo.Object),
            new ObterOrdemServicoDetalhadaUseCase(oficinaRepo.Object, new ObterOrcamentoDetalhadoUseCase(oficinaRepo.Object, catalogoRepo.Object)),
            new ListarOrdensServicoUseCase(oficinaRepo.Object),
            new FinalizarOrdemServicoUseCase(oficinaRepo.Object),
            new EntregarOrdemServicoUseCase(oficinaRepo.Object));
    }
}
