using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Oficina.Domain.Oficina;
using Oficina.Infrastructure.Persistencia;
using Xunit;

namespace Oficina.Tests.Infrastructure.Persistencia;

public class OrcamentoMapTests
{
    [Fact]
    public void OrcamentoItens_DevemUsarIdsGeradosPelaAplicacao()
    {
        using var db = new OficinaDbContext(new DbContextOptionsBuilder<OficinaDbContext>()
            .UseSqlServer("Server=(local);Database=OficinaTests;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options);

        Assert.Equal(ValueGenerated.Never, ObterValueGenerated(db, typeof(OrcamentoItemServico)));
        Assert.Equal(ValueGenerated.Never, ObterValueGenerated(db, typeof(OrcamentoItemMaterial)));
    }

    private static ValueGenerated ObterValueGenerated(OficinaDbContext db, Type tipo)
        => db.Model.FindEntityType(tipo)!.FindProperty("Id")!.ValueGenerated;
}
