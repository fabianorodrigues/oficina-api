using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Infrastructure.Persistencia;
using Xunit;

namespace Oficina.Tests.Infrastructure.Persistencia;

public class ServicoMapTests
{
    [Fact]
    public void ServicoItensRequeridos_DevemUsarIdsGeradosPelaAplicacao()
    {
        using var db = new OficinaDbContext(new DbContextOptionsBuilder<OficinaDbContext>()
            .UseSqlServer("Server=(local);Database=OficinaTests;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options);

        Assert.Equal(ValueGenerated.Never, ObterValueGenerated(db, typeof(ServicoPecaRequerida)));
        Assert.Equal(ValueGenerated.Never, ObterValueGenerated(db, typeof(ServicoInsumoRequerido)));
    }

    private static ValueGenerated ObterValueGenerated(OficinaDbContext db, Type tipo)
        => db.Model.FindEntityType(tipo)!.FindProperty(nameof(ServicoPecaRequerida.Id))!.ValueGenerated;
}
