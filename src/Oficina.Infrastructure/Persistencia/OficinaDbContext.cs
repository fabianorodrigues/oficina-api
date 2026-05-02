using Microsoft.EntityFrameworkCore;
using Oficina.Infrastructure.Persistencia.Mapeamentos;
using Oficina.Domain.Cadastro;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;
using Oficina.Domain.Seguranca;

namespace Oficina.Infrastructure.Persistencia;

public class OficinaDbContext : DbContext
{
    public OficinaDbContext(DbContextOptions<OficinaDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<EstoquePeca> EstoquePecas => Set<EstoquePeca>();
    public DbSet<EstoqueInsumo> EstoqueInsumos => Set<EstoqueInsumo>();

    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ClienteMap());
        modelBuilder.ApplyConfiguration(new VeiculoMap());

        modelBuilder.ApplyConfiguration(new ServicoMap());
        modelBuilder.ApplyConfiguration(new PecaMap());
        modelBuilder.ApplyConfiguration(new InsumoMap());
        modelBuilder.ApplyConfiguration(new EstoquePecaMap());
        modelBuilder.ApplyConfiguration(new EstoqueInsumoMap());

        modelBuilder.ApplyConfiguration(new OrdemServicoMap());
        modelBuilder.ApplyConfiguration(new OrcamentoMap());
        modelBuilder.ApplyConfiguration(new FuncionarioMap());
    }
}
