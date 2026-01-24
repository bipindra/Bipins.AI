using Bipins.AI.Core.Ingestion;
using Xunit;

namespace Bipins.AI.UnitTests.Ingestion;

public class TenantValidatorTests
{
    [Fact]
    public void IsValid_WithValidTenantId_ReturnsTrue()
    {
        Assert.True(TenantValidator.IsValid("tenant-123"));
        Assert.True(TenantValidator.IsValid("tenant_456"));
        Assert.True(TenantValidator.IsValid("Tenant789"));
        Assert.True(TenantValidator.IsValid("tenant123"));
    }

    [Fact]
    public void IsValid_WithInvalidCharacters_ReturnsFalse()
    {
        Assert.False(TenantValidator.IsValid("tenant@123"));
        Assert.False(TenantValidator.IsValid("tenant 123"));
        Assert.False(TenantValidator.IsValid("tenant#123"));
        Assert.False(TenantValidator.IsValid("tenant.123"));
    }

    [Fact]
    public void IsValid_WithNull_ReturnsFalse()
    {
        Assert.False(TenantValidator.IsValid(null));
    }

    [Fact]
    public void IsValid_WithEmptyString_ReturnsFalse()
    {
        Assert.False(TenantValidator.IsValid(""));
        Assert.False(TenantValidator.IsValid("   "));
    }

    [Fact]
    public void IsValid_WithTooLongTenantId_ReturnsFalse()
    {
        var longTenantId = new string('a', 101);
        Assert.False(TenantValidator.IsValid(longTenantId));
    }

    [Fact]
    public void ValidateOrThrow_WithValidTenantId_DoesNotThrow()
    {
        TenantValidator.ValidateOrThrow("tenant-123");
    }

    [Fact]
    public void ValidateOrThrow_WithInvalidTenantId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TenantValidator.ValidateOrThrow("tenant@123"));
        Assert.Throws<ArgumentException>(() => TenantValidator.ValidateOrThrow(null));
        Assert.Throws<ArgumentException>(() => TenantValidator.ValidateOrThrow(""));
    }
}
