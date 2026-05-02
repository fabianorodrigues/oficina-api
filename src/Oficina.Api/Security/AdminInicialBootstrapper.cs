using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Domain.Seguranca;
using Oficina.Domain.Seguranca.Enums;

namespace Oficina.Api.Security;

public static class AdminInicialBootstrapper
{
    public static async Task GarantirAdminInicial(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var habilitado = env.IsDevelopment() || config.GetValue<bool>("AdminInicial:Enabled");
        if (!habilitado)
            return;

        var nome = config["AdminInicial:Nome"];
        var cpf = config["AdminInicial:Cpf"];
        var senha = config["AdminInicial:Senha"];
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(cpf) || string.IsNullOrWhiteSpace(senha))
            return;

        var repo = scope.ServiceProvider.GetRequiredService<IFuncionarioRepository>();
        var passwordHash = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
        var cpfNormalizado = Funcionario.NormalizarCpf(cpf);

        if (await repo.ExistePorCpf(cpfNormalizado, CancellationToken.None))
            return;

        var admin = new Funcionario(nome, cpfNormalizado, passwordHash.Hash(senha), PerfilUsuarioInterno.Admin);
        await repo.Adicionar(admin, CancellationToken.None);
        await repo.Salvar(CancellationToken.None);
    }
}
