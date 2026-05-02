using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.ValueObjects;

namespace Oficina.Infrastructure.Persistencia.Mapeamentos;

public class OrdemServicoMap : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> b)
    {
        b.ToTable("OrdensServico");
        b.HasKey(x => x.Id);

        b.Property(x => x.VeiculoId).IsRequired();
        b.Property(x => x.TipoManutencao).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.OrigemUltimaAtualizacaoStatus).IsRequired();
        b.Property(x => x.DataUltimaAtualizacaoStatus).IsRequired();
        b.Property(x => x.DataCriacao).IsRequired();
        b.Property(x => x.DataInicioExecucao);
        b.Property(x => x.DataFimExecucao);
        b.Property(x => x.OrcamentoId);

        b.OwnsMany(x => (ICollection<ItemServicoOs>)x.ItensServico, items =>
        {
            items.ToTable("ItensServicoOs");
            items.WithOwner().HasForeignKey("OrdemServicoId");
            items.HasKey(x => x.Id);
            items.Property(x => x.ServicoId).IsRequired();
        });

        b.OwnsOne(x => x.Diagnostico, d =>
        {
            d.ToTable("Diagnosticos");

            d.WithOwner().HasForeignKey("OrdemServicoId");

            d.Property<Guid>("OrdemServicoId");
            d.HasKey("OrdemServicoId");

            d.Property(x => x.Descricao).HasMaxLength(2000).IsRequired();
            d.Property(x => x.DataRegistro).IsRequired();
        });
    }
}
