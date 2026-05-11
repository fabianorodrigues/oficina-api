using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Oficina.Api.Security;
using Xunit;

namespace Oficina.Tests.Api.Security;

public class AdminInicialBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_QuandoAdminInicialDesabilitado_NaoDeveExecutarBootstrap()
    {
        var bootstrapper = new BootstrapperFake();
        using var service = CriarService(bootstrapper, new Dictionary<string, string?>
        {
            ["AdminInicial:Enabled"] = "false"
        });

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, bootstrapper.Chamadas);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoBootstrapFalharTemporariamente_NaoDeveDerrubarServico()
    {
        var bootstrapper = new BootstrapperFake
        {
            FalhasAntesDoSucesso = 1
        };
        using var service = CriarService(bootstrapper, ConfigAdminHabilitado());

        await service.StartAsync(CancellationToken.None);

        var concluiu = await bootstrapper.AguardarSucesso();
        await service.StopAsync(CancellationToken.None);

        Assert.True(concluiu);
        Assert.Equal(2, bootstrapper.Chamadas);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoAdminInicialHabilitado_DeveExecutarBootstrap()
    {
        var bootstrapper = new BootstrapperFake();
        using var service = CriarService(bootstrapper, ConfigAdminHabilitado());

        await service.StartAsync(CancellationToken.None);

        var concluiu = await bootstrapper.AguardarSucesso();
        await service.StopAsync(CancellationToken.None);

        Assert.True(concluiu);
        Assert.Equal(1, bootstrapper.Chamadas);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoHabilitadoSemDadosObrigatorios_NaoDeveExecutarBootstrap()
    {
        var bootstrapper = new BootstrapperFake();
        using var service = CriarService(bootstrapper, new Dictionary<string, string?>
        {
            ["AdminInicial:Enabled"] = "true",
            ["AdminInicial:Nome"] = "",
            ["AdminInicial:Cpf"] = "",
            ["AdminInicial:Senha"] = ""
        });

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, bootstrapper.Chamadas);
    }

    private static AdminInicialBackgroundService CriarService(
        BootstrapperFake bootstrapper,
        Dictionary<string, string?> config)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        var options = Options.Create(new AdminInicialBootstrapOptions
        {
            MaxTentativas = 3,
            IntervaloSegundos = 0
        });

        return new AdminInicialBackgroundService(
            configuration,
            bootstrapper,
            options,
            NullLogger<AdminInicialBackgroundService>.Instance);
    }

    private static Dictionary<string, string?> ConfigAdminHabilitado()
        => new()
        {
            ["AdminInicial:Enabled"] = "true",
            ["AdminInicial:Nome"] = "Admin Inicial",
            ["AdminInicial:Cpf"] = "39053344705",
            ["AdminInicial:Senha"] = "Senha@123"
        };

    private sealed class BootstrapperFake : IAdminInicialBootstrapper
    {
        private readonly TaskCompletionSource<bool> _sucesso = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Chamadas { get; private set; }

        public int FalhasAntesDoSucesso { get; init; }

        public Task GarantirAdminInicial(CancellationToken ct)
        {
            Chamadas++;

            if (Chamadas <= FalhasAntesDoSucesso)
                throw new InvalidOperationException("falha temporaria");

            _sucesso.TrySetResult(true);
            return Task.CompletedTask;
        }

        public async Task<bool> AguardarSucesso()
        {
            var completed = await Task.WhenAny(_sucesso.Task, Task.Delay(1000));
            return completed == _sucesso.Task && await _sucesso.Task;
        }
    }
}
