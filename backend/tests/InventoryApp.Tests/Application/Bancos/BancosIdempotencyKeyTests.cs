using InventoryApp.Application.Bancos;
using Xunit;

namespace InventoryApp.Tests.Application.Bancos;

public class BancosIdempotencyKeyTests
{
    [Fact]
    public void Create_WithValidKey_ReturnsNormalizedKey()
    {
        var key = BancosIdempotencyKey.Create("  valid-key_123  ");
        Assert.Equal("valid-key_123", key.Value);
        Assert.Equal("valid-key_123", key.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhiteSpace_ThrowsArgumentException(string invalidKey)
    {
        Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create(invalidKey));
    }

    [Fact]
    public void Create_WithOversizedKey_ThrowsArgumentException()
    {
        var oversizedKey = new string('a', BancosIdempotencyKey.MaxLength + 1);
        Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create(oversizedKey));
    }

    [Theory]
    [InlineData("invalid key")]
    [InlineData("invalid!key")]
    [InlineData("invalid@key")]
    [InlineData("invalid#key")]
    [InlineData("invalid/key")]
    [InlineData("invalid\nkey")]
    [InlineData("invalid\rkey")]
    [InlineData("invalid\0key")]
    public void Create_WithUnsafeCharacters_ThrowsArgumentException(string unsafeKey)
    {
        Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create(unsafeKey));
    }

    [Fact]
    public void ValueEquality_UsesNormalizedValue()
    {
        Assert.Equal(
            BancosIdempotencyKey.Create("test-key-1"),
            BancosIdempotencyKey.Create("test-key-1"));
        Assert.NotEqual(
            BancosIdempotencyKey.Create("test-key-1"),
            BancosIdempotencyKey.Create("test-key-2"));
    }
}
