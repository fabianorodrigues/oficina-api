using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.UseCases.Cadastro;
using Oficina.Application.UseCases.CatalogoEstoque;
using Oficina.Application.UseCases.Oficina;
using Oficina.Application.UseCases.Seguranca;
using Oficina.Application.Abstractions.Seguranca;

namespace Oficina.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        object value = services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Cadastro
        services.AddScoped<CadastrarClienteUseCase>();
        services.AddScoped<AtualizarClienteUseCase>();
        services.AddScoped<ListarClientesUseCase>();
        services.AddScoped<CadastrarVeiculoUseCase>();
        services.AddScoped<AtualizarVeiculoUseCase>();
        services.AddScoped<ObterClienteUseCase>();
        services.AddScoped<ListarVeiculosUseCase>();
        services.AddScoped<ObterVeiculoUseCase>();
        services.AddScoped<ListarVeiculosPorClienteUseCase>();

        // Catálogo & Estoque
        services.AddScoped<CadastrarServicoUseCase>();
        services.AddScoped<ListarServicosUseCase>();
        services.AddScoped<ObterServicoUseCase>();
        services.AddScoped<AtualizarServicoUseCase>();
        services.AddScoped<CadastrarPecaUseCase>();
        services.AddScoped<ListarPecasUseCase>();
        services.AddScoped<ObterPecaUseCase>();
        services.AddScoped<AtualizarPecaUseCase>();
        services.AddScoped<CadastrarInsumoUseCase>();
        services.AddScoped<ListarInsumosUseCase>();
        services.AddScoped<ObterInsumoUseCase>();
        services.AddScoped<AtualizarInsumoUseCase>();
        services.AddScoped<ListarEstoqueUseCase>();
        services.AddScoped<ObterEstoquePecaUseCase>();
        services.AddScoped<ObterEstoqueInsumoUseCase>();
        services.AddScoped<AjustarEstoquePecaUseCase>();
        services.AddScoped<AjustarEstoqueInsumoUseCase>();

        // Oficina
        services.AddScoped<AbrirOrdemServicoUseCase>();
        services.AddScoped<CriarOsPreventivaUseCase>();
        services.AddScoped<CriarOsCorretivaUseCase>();
        services.AddScoped<RegistrarDiagnosticoUseCase>();
        services.AddScoped<ClassificarOrdemServicoUseCase>();
        services.AddScoped<AprovarOrcamentoUseCase>();
        services.AddScoped<RecusarOrcamentoUseCase>();
        services.AddScoped<ProcessarAcaoExternaOrcamentoUseCase>();
        services.AddScoped<ObterOrdemServicoUseCase>();
        services.AddScoped<ObterStatusOrdemServicoUseCase>();
        services.AddScoped<ObterOrdemServicoDetalhadaUseCase>();
        services.AddScoped<ListarOrdensServicoUseCase>();
        services.AddScoped<FinalizarOrdemServicoUseCase>();
        services.AddScoped<EntregarOrdemServicoUseCase>();
        services.AddScoped<ObterOrcamentoUseCase>();
        services.AddScoped<ObterOrcamentoDetalhadoUseCase>();
        services.AddScoped<RelatorioTempoMedioExecucaoUseCase>();
        services.AddScoped<ListarMinhasOrdensServicoUseCase>();
        services.AddScoped<AprovarMeuOrcamentoUseCase>();
        services.AddScoped<RecusarMeuOrcamentoUseCase>();

        // Seguranca
        services.AddScoped<IClienteOwnershipService, ClienteOwnershipService>();
        services.AddScoped<AutenticarClienteUseCase>();
        services.AddScoped<AutenticarFuncionarioUseCase>();
        services.AddScoped<CriarFuncionarioUseCase>();
        services.AddScoped<ListarFuncionariosUseCase>();
        services.AddScoped<ObterFuncionarioUseCase>();
        services.AddScoped<AtualizarFuncionarioUseCase>();
        services.AddScoped<AlterarSenhaFuncionarioUseCase>();
        services.AddScoped<AlterarStatusFuncionarioUseCase>();

        return services;
    }
}
