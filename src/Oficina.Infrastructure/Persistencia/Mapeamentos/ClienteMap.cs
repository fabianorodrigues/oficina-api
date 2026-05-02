using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Cadastro;

namespace Oficina.Infrastructure.Persistencia.Mapeamentos;

public class ClienteMap : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("Clientes");
        b.HasKey(x => x.Id);

        b.OwnsOne(x => x.Documento, doc =>
        {
            doc.Property(x => x.Valor).HasColumnName("CpfCnpj").HasMaxLength(14).IsRequired();
        });

        b.OwnsOne(x => x.Documento, doc =>
        {
            doc.Property(p => p.Valor)
               .HasColumnName("Documento")
               .HasMaxLength(14)
               .IsRequired();

            doc.HasIndex(p => p.Valor).IsUnique();
        });

        b.Property(x => x.Nome)
        .HasMaxLength(150)
        .IsRequired();

        b.OwnsOne(x => x.Contato, contato =>
        {
            contato.Property(c => c.Email)
                .HasColumnName("ContatoEmail")
                .HasMaxLength(150)
                .IsRequired();

            contato.Property(c => c.Telefone)
                .HasColumnName("ContatoTelefone")
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}
