using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.CatalogoEstoque;

namespace Oficina.Infrastructure.Persistencia.Mapeamentos;

public class EstoqueInsumoMap : IEntityTypeConfiguration<EstoqueInsumo>
{
    public void Configure(EntityTypeBuilder<EstoqueInsumo> b)
    {
        b.ToTable("EstoqueInsumos");
        b.HasKey(x => x.Id);

        b.Property(x => x.InsumoId).IsRequired();
        b.Property(x => x.Quantidade).IsRequired();

        b.HasIndex(x => x.InsumoId).IsUnique();
    }
}
