using PetService.Domain.Entities;

namespace PetService.Domain.Tests;

public sealed class OwnerTests
{
    [Fact]
    public void Constructor_NormalizesValidOwnerData()
    {
        var owner = new Owner("  Test Owner  ", "TEST.OWNER@example.com", "+389 70 123 456", "  Skopje  ");

        Assert.NotEqual(Guid.Empty, owner.OwnerId);
        Assert.Equal("Test Owner", owner.OwnerName);
        Assert.Equal("test.owner@example.com", owner.Email);
        Assert.Equal("+389 70 123 456", owner.Phone);
        Assert.Equal("Skopje", owner.Address);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("name@example.com trailing")]
    public void Constructor_WithInvalidEmail_Throws(string email) =>
        Assert.Throws<ArgumentException>(() => new Owner("Test Owner", email, "+38970123456", null));

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("+389-ABC-123")]
    [InlineData("++38970123456")]
    public void Constructor_WithInvalidPhone_Throws(string phone) =>
        Assert.Throws<ArgumentException>(() => new Owner("Test Owner", "test.owner@example.com", phone, null));

    [Fact]
    public void Update_WithBlankAddress_ClearsAddress()
    {
        var owner = new Owner("Test Owner", "test.owner@example.com", "+38970123456", "Skopje");

        owner.Update("Test Owner", "test.owner@example.com", "+38970123456", "  ");

        Assert.Null(owner.Address);
    }
}
