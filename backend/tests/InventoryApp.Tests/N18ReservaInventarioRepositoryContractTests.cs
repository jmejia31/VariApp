using InventoryApp.Application.Interfaces;
using InventoryApp.Infrastructure.Repositories;
using Xunit;

namespace InventoryApp.Tests;

public sealed class N18ReservaInventarioRepositoryContractTests
{
    [Fact]
    public void Repository_ImplementaContratoCanónico()
    {
        Assert.Contains(typeof(IReservaInventarioRepository), typeof(ReservaInventarioRepository).GetInterfaces());
    }

    [Fact]
    public void Contrato_ExponeConsultaPaginadaLecturaTrackingYNumeroUnico()
    {
        var metodos = typeof(IReservaInventarioRepository).GetMethods().Select(x => x.Name).ToHashSet();
        Assert.Contains("GetPagedAsync", metodos);
        Assert.Contains("GetByIdAsync", metodos);
        Assert.Contains("ExisteNumeroAsync", metodos);
        Assert.Contains("AddAsync", metodos);
        Assert.Contains("SaveChangesAsync", metodos);
    }
}
