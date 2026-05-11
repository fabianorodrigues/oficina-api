using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Oficina.Api.Security;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Domain.Seguranca;
using Oficina.Domain.Seguranca.Enums;
using Xunit;

namespace Oficina.Tests.Api.Security;

public class AdminInicialBootstrapperTests
{
    [Fact]
    public async Task GarantirAdminInicial_QuandoDesabilitado_NaoDeveCriarAdmin()
    {
        var repo = new FuncionarioRepositoryFake();
        var provider = CriarProvider(repo, new PasswordHashServiceFake(), new Dictionary<string, string?>
        {
            ["AdminInicial:Enabled"] = "false"
        });

        await AdminInicialBootstrapper.GarantirAdminInicial(provider, CancellationToken.None);

        Assert.Empty(repo.Adicionados);
        Assert.Equal(0, repo.SaveCount);
    }

    [Fact]
    public async Task GarantirAdminInicial_QuandoHabilitadoSemDados_DeveFalharComErroSeguro()
    {
        var repo = new FuncionarioRepositoryFake();
        var provider = CriarProvider(repo, new PasswordHashServiceFake(), new Dictionary<string, string?>
        {
            ["AdminInicial:Enabled"] = "true",
            ["AdminInicial:Nome"] = "",
            ["AdminInicial:Cpf"] = "",
            ["AdminInicial:Senha"] = ""
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AdminInicialBootstrapper.GarantirAdminInicial(provider, CancellationToken.None));

        Assert.Contains("configuracao obrigatoria", ex.Message);
        Assert.DoesNotContain("Cpf", ex.Message);
        Assert.DoesNotContain("Senha", ex.Message);
        Assert.Empty(repo.Adicionados);
    }

    [Fact]
    public async Task GarantirAdminInicial_QuandoCpfJaExiste_NaoDeveRecriarNemSobrescreverSenha()
    {
        var existente = new Funcionario("Admin Existente", "39053344705", "hash-antigo", PerfilUsuarioInterno.Admin);
        var repo = new FuncionarioRepositoryFake { Existente = existente };
        var passwordHash = new PasswordHashServiceFake();
        var provider = CriarProvider(repo, passwordHash, ConfigAdminHabilitado());

        await AdminInicialBootstrapper.GarantirAdminInicial(provider, CancellationToken.None);

        Assert.Empty(repo.Adicionados);
        Assert.Equal(0, repo.SaveCount);
        Assert.Equal(0, passwordHash.HashCalls);
        Assert.Equal("hash-antigo", existente.SenhaHash);
    }

    [Fact]
    public async Task GarantirAdminInicial_QuandoDadosValidos_DeveCriarAdmin()
    {
        var repo = new FuncionarioRepositoryFake();
        var passwordHash = new PasswordHashServiceFake();
        var provider = CriarProvider(repo, passwordHash, ConfigAdminHabilitado());

        await AdminInicialBootstrapper.GarantirAdminInicial(provider, CancellationToken.None);

        var admin = Assert.Single(repo.Adicionados);
        Assert.Equal("Admin Inicial", admin.Nome);
        Assert.Equal("39053344705", admin.Cpf);
        Assert.Equal("hash-seguro", admin.SenhaHash);
        Assert.Equal(PerfilUsuarioInterno.Admin, admin.Perfil);
        Assert.Equal(1, repo.SaveCount);
        Assert.Equal(1, passwordHash.HashCalls);
    }

    [Fact]
    public async Task GarantirAdminInicial_QuandoDuplicidadeConcorrenteConfirmada_DeveTratarComoAdminExistente()
    {
        var existente = new Funcionario("Admin Existente", "39053344705", "hash-antigo", PerfilUsuarioInterno.Admin);
        var repo = new FuncionarioRepositoryFake
        {
            SalvarException = new DbUpdateException(
                "Cannot insert duplicate key row in object 'dbo.Funcionarios' with unique index 'IX_Funcionarios_Cpf'.",
                new InvalidOperationException("duplicate key")),
            ExistenteAposConcorrencia = existente
        };
        var provider = CriarProvider(repo, new PasswordHashServiceFake(), ConfigAdminHabilitado());

        await AdminInicialBootstrapper.GarantirAdminInicial(provider, CancellationToken.None);

        Assert.Single(repo.Adicionados);
        Assert.Equal(1, repo.SaveCount);
        Assert.Equal("hash-antigo", existente.SenhaHash);
    }

    [Fact]
    public async Task GarantirAdminInicial_QuandoErroDeBancoNaoForDuplicidade_DevePropagarErro()
    {
        var repo = new FuncionarioRepositoryFake
        {
            SalvarException = new DbUpdateException("Falha de banco.", new InvalidOperationException("timeout"))
        };
        var provider = CriarProvider(repo, new PasswordHashServiceFake(), ConfigAdminHabilitado());

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            AdminInicialBootstrapper.GarantirAdminInicial(provider, CancellationToken.None));
    }

    private static Dictionary<string, string?> ConfigAdminHabilitado()
        => new()
        {
            ["AdminInicial:Enabled"] = "true",
            ["AdminInicial:Nome"] = "Admin Inicial",
            ["AdminInicial:Cpf"] = "39053344705",
            ["AdminInicial:Senha"] = "Senha@123"
        };

    private static ServiceProvider CriarProvider(
        IFuncionarioRepository repo,
        IPasswordHashService passwordHash,
        Dictionary<string, string?> config)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(repo)
            .AddSingleton(passwordHash)
            .BuildServiceProvider();
    }

    private sealed class FuncionarioRepositoryFake : IFuncionarioRepository
    {
        public Funcionario? Existente { get; set; }
        public Funcionario? ExistenteAposConcorrencia { get; set; }
        public Exception? SalvarException { get; set; }
        public List<Funcionario> Adicionados { get; } = [];
        public int SaveCount { get; private set; }

        public Task<Funcionario?> ObterPorId(Guid id, CancellationToken ct)
            => Task.FromResult<Funcionario?>(null);

        public Task<Funcionario?> ObterPorCpf(string cpfNormalizado, CancellationToken ct)
            => Task.FromResult(Existente ?? ExistenteAposConcorrencia);

        public Task<bool> ExistePorCpf(string cpfNormalizado, CancellationToken ct)
            => Task.FromResult(Existente is not null);

        public Task<IReadOnlyList<Funcionario>> Listar(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Funcionario>>([]);

        public Task Adicionar(Funcionario funcionario, CancellationToken ct)
        {
            Adicionados.Add(funcionario);
            return Task.CompletedTask;
        }

        public Task Salvar(CancellationToken ct)
        {
            SaveCount++;
            if (SalvarException is not null)
                throw SalvarException;

            return Task.CompletedTask;
        }
    }

    private sealed class PasswordHashServiceFake : IPasswordHashService
    {
        public int HashCalls { get; private set; }

        public string Hash(string senha)
        {
            HashCalls++;
            return "hash-seguro";
        }

        public bool Verificar(string senhaHash, string senha) => false;
    }
}
