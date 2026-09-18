using InventoryApp.Application.Bancos;
using InventoryApp.Application.Interfaces;
using Xunit;

namespace InventoryApp.Tests.Application;

public sealed class N818BackendArchitectureTests
{
    [Fact]
    public void Banking_use_case_is_owned_by_domain_slice_and_preserves_interface_contract()
    {
        Assert.Equal("InventoryApp.Application.Bancos", typeof(OperacionBancariaService).Namespace);
        Assert.Contains(typeof(IOperacionBancariaService), typeof(OperacionBancariaService).GetInterfaces());
        Assert.Equal("InventoryApp.Application.Bancos", typeof(BancosIdempotencyKey).Namespace);
        Assert.Equal(100, BancosIdempotencyKey.MaxLength);
    }

    [Fact]
    public void Banking_idempotency_key_fails_closed_on_unsafe_input()
    {
        Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create("bad key"));
        Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create(" "));
        Assert.Equal("safe-key_1", BancosIdempotencyKey.Create("safe-key_1").Value);
    }
}
