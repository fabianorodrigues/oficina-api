using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.CatalogoEstoque;

namespace Oficina.Infrastructure.Persistencia.Mapeamentos;

public class EstoquePecaMap : IEntityTypeConfiguration<EstoquePeca>
{
    public void Configure(EntityTypeBuilder<EstoquePeca> b)
    {
        b.ToTable("EstoquePecas");
        b.HasKey(x => x.Id);

        b.Property(x => x.PecaId).IsRequired();
        b.Property(x => x.Quantidade).IsRequired();

        b.HasIndex(x => x.PecaId).IsUnique();
    }
}
