using Microsoft.Extensions.Options;

namespace Oficina.Api.Security;

public sealed class AdminInicialBackgroundService(
    IConfiguration configuration,
    IAdminInicialBootstrapper bootstrapper,
    IOptions<AdminInicialBootstrapOptions> options,
    ILogger<AdminInicialBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>("AdminInicial:Enabled"))
        {
            logger.LogInformation("Bootstrap do admin inicial desabilitado.");
            return;
        }

        if (!ConfiguracaoObrigatoriaPresente())
        {
            logger.LogError("Admin inicial habilitado, mas configuracao obrigatoria esta ausente.");
            return;
        }

        var maxTentativas = Math.Max(1, options.Value.MaxTentativas);
        var intervalo = options.Value.Intervalo;

        await Task.Yield();

        for (var tentativa = 1; tentativa <= maxTentativas && !stoppingToken.IsCancellationRequested; tentativa++)
        {
            try
            {
                await bootstrapper.GarantirAdminInicial(stoppingToken);
                logger.LogInformation("Bootstrap do admin inicial concluido.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (tentativa < maxTentativas)
            {
                logger.LogWarning(
                    "Bootstrap do admin inicial falhou na tentativa {Tentativa}/{MaxTentativas}. Nova tentativa em {IntervaloSegundos}s. Tipo: {ExceptionType}.",
                    tentativa,
                    maxTentativas,
                    intervalo.TotalSeconds,
                    ex.GetType().Name);

                if (intervalo > TimeSpan.Zero)
                    await Task.Delay(intervalo, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Bootstrap do admin inicial falhou apos {MaxTentativas} tentativas. Tipo: {ExceptionType}.",
                    maxTentativas,
                    ex.GetType().Name);
                return;
            }
        }
    }

    private bool ConfiguracaoObrigatoriaPresente()
        => !string.IsNullOrWhiteSpace(configuration["AdminInicial:Nome"]) &&
           !string.IsNullOrWhiteSpace(configuration["AdminInicial:Cpf"]) &&
           !string.IsNullOrWhiteSpace(configuration["AdminInicial:Senha"]);
}
