using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Domain.Seguranca;
using Oficina.Domain.Seguranca.Enums;

namespace Oficina.Api.Security;

public static class AdminInicialBootstrapper
{
    public static async Task GarantirAdminInicial(WebApplication app)
    {
        await GarantirAdminInicial(app.Services, CancellationToken.None);
    }

    public static async Task GarantirAdminInicial(IServiceProvider services, CancellationToken ct)
    {
        using var scope = services.CreateScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Oficina.Api.Security.AdminInicialBootstrapper");
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var habilitado = config.GetValue<bool>("AdminInicial:Enabled");
        if (!habilitado)
            return;

        var nome = config["AdminInicial:Nome"];
        var cpf = config["AdminInicial:Cpf"];
        var senha = config["AdminInicial:Senha"];
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(cpf) || string.IsNullOrWhiteSpace(senha))
            throw new InvalidOperationException("Admin inicial habilitado, mas configuracao obrigatoria esta ausente.");

        var repo = scope.ServiceProvider.GetRequiredService<IFuncionarioRepository>();
        var passwordHash = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
        var cpfNormalizado = Funcionario.NormalizarCpf(cpf);

        if (await repo.ExistePorCpf(cpfNormalizado, ct))
        {
            logger.LogInformation("Admin inicial ja existe. Bootstrap ignorado.");
            return;
        }

        var admin = new Funcionario(nome, cpfNormalizado, passwordHash.Hash(senha), PerfilUsuarioInterno.Admin);
        await repo.Adicionar(admin, ct);

        try
        {
            await repo.Salvar(ct);
            logger.LogInformation("Admin inicial criado com sucesso.");
        }
        catch (DbUpdateException ex) when (EhDuplicidadeCpfFuncionario(ex))
        {
            if (await repo.ObterPorCpf(cpfNormalizado, ct) is not null)
            {
                logger.LogInformation("Admin inicial ja existe apos tentativa concorrente. Bootstrap ignorado.");
                return;
            }

            throw;
        }
    }

    private static bool EhDuplicidadeCpfFuncionario(DbUpdateException ex)
    {
        var message = ex.ToString();
        if (!message.Contains("IX_Funcionarios_Cpf", StringComparison.OrdinalIgnoreCase) &&
            !(message.Contains("Funcionarios", StringComparison.OrdinalIgnoreCase) &&
              message.Contains("Cpf", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var exception = ex.InnerException;
        while (exception is not null)
        {
            var numberProperty = exception.GetType().GetProperty("Number");
            if (numberProperty?.GetValue(exception) is int number)
                return number is 2601 or 2627;

            exception = exception.InnerException;
        }

        return message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("duplic", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }
}
