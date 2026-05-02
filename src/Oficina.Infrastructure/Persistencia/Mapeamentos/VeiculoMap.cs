using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Cadastro;

namespace Oficina.Infrastructure.Persistencia.Mapeamentos;

public class VeiculoMap : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> b)
    {
        b.ToTable("Veiculos");
        b.HasKey(x => x.Id);

        b.Property(x => x.ClienteId).IsRequired();

        b.OwnsOne(x => x.Placa, placa =>
        {
            placa.Property(x => x.Valor).HasColumnName("Placa").HasMaxLength(8).IsRequired();
        });

        b.OwnsOne(x => x.Renavam, renavam =>
        {
            renavam.Property(x => x.Valor).HasColumnName("Renavam").HasMaxLength(11).IsRequired();
        });

        b.OwnsOne(x => x.Placa, placa =>
        {
            placa.Property(p => p.Valor)
                 .HasColumnName("Placa")
                 .HasMaxLength(7)
                 .IsRequired();

            placa.HasIndex(p => p.Valor).IsUnique();
        });

        b.OwnsOne(x => x.Renavam, renavam =>
        {
            renavam.Property(p => p.Valor)
                   .HasColumnName("Renavam")
                   .HasMaxLength(11)
                   .IsRequired();

            renavam.HasIndex(p => p.Valor).IsUnique();
        });

        b.OwnsOne(x => x.Modelo, modelo =>
        {
            modelo.Property(p => p.Descricao)
                .HasColumnName("ModeloDescricao")
                .HasMaxLength(100)
                .IsRequired();

            modelo.Property(p => p.Marca)
                .HasColumnName("ModeloMarca")
                .HasMaxLength(100)
                .IsRequired();

            modelo.Property(p => p.Ano)
                .HasColumnName("ModeloAno")
                .IsRequired();
        });
    }
}
