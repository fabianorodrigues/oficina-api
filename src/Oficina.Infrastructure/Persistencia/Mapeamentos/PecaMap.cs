using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.CatalogoEstoque;

namespace Oficina.Infrastructure.Persistencia.Mapeamentos;

public class PecaMap : IEntityTypeConfiguration<Peca>
{
    public void Configure(EntityTypeBuilder<Peca> b)
    {
        b.ToTable("Pecas");
        b.HasKey(x => x.Id);
        b.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
        b.Property(x => x.PrecoUnitario).HasColumnType("decimal(18,2)").IsRequired();
    }
}
