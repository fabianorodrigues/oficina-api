using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Oficina;

namespace Oficina.Infrastructure.Persistencia.Mapeamentos;

public class OrcamentoMap : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> b)
    {
        b.ToTable("Orcamentos");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrdemServicoId).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.ValorTotal).HasColumnType("decimal(18,2)").IsRequired();
        b.Property(x => x.DataCriacao).IsRequired();
        b.Property(x => x.TokenAcaoExterna).HasMaxLength(200);
        b.Property(x => x.TokenAcaoExternaExpiraEm);
        b.HasIndex(x => x.TokenAcaoExterna).IsUnique();

        b.HasIndex(x => x.OrdemServicoId).IsUnique();

        b.OwnsMany(x => (ICollection<OrcamentoItemServico>)x.ItensServico, items =>
        {
            items.ToTable("OrcamentoItensServico");
            items.WithOwner().HasForeignKey("OrcamentoId");
            items.HasKey(x => x.Id);
            items.Property(x => x.ServicoId).IsRequired();
            items.Property(x => x.ValorMaoDeObra).HasColumnType("decimal(18,2)").IsRequired();
        });

        b.OwnsMany(x => (ICollection<OrcamentoItemMaterial>)x.ItensMaterial, items =>
        {
            items.ToTable("OrcamentoItensMaterial");
            items.WithOwner().HasForeignKey("OrcamentoId");
            items.HasKey(x => x.Id);
            items.Property(x => x.Tipo).IsRequired();
            items.Property(x => x.MaterialId).IsRequired();
            items.Property(x => x.Quantidade).IsRequired();
            items.Property(x => x.ValorUnitario).HasColumnType("decimal(18,2)").IsRequired();
        });
    }
}
