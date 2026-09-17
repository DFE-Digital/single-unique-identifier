using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.UnitTests;

public class SupplierWebhookTests
{
    private const string ValidSupplierId = "SUPPLIER-1";
    private const string ValidEndpoint = "https://example.com/webhook";
    private const string ValidContract = "v1";
    private const string ValidKvRef = "kv-secret-ref";

    [Fact]
    public void Constructor_ValidInputs_CreatesInstance_AndAssignsProperties()
    {
        // Act
        var webhook = new SupplierWebhook(
            ValidSupplierId,
            ValidEndpoint,
            true,
            ValidContract,
            ValidKvRef
        );

        // Assert
        Assert.Equal(ValidSupplierId, webhook.SupplierId);
        Assert.Equal(ValidEndpoint, webhook.EndpointUrl);
        Assert.True(webhook.IsEnabled);
        Assert.Equal(ValidContract, webhook.ContractVersion);
        Assert.Equal(ValidKvRef, webhook.SecretKeyVaultReference);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidSupplierId_ThrowsArgumentException(string invalidSupplierId)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SupplierWebhook(invalidSupplierId, ValidEndpoint, true, ValidContract, ValidKvRef)
        );

        Assert.Equal("supplierId", ex.ParamName);
        Assert.Contains("Supplier ID cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidContractVersion_ThrowsArgumentException(string invalidContract)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SupplierWebhook(ValidSupplierId, ValidEndpoint, true, invalidContract, ValidKvRef)
        );

        Assert.Equal("contractVersion", ex.ParamName);
        Assert.Contains("Contract version cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidKeyVaultReference_ThrowsArgumentException(string invalidKvRef)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SupplierWebhook(ValidSupplierId, ValidEndpoint, true, ValidContract, invalidKvRef)
        );

        Assert.Equal("secretKeyVaultReference", ex.ParamName);
        Assert.Contains("Key Vault reference cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("/relative/path/webhook")]
    [InlineData("http://insecure.com/webhook")] // Fails the HTTPS check
    [InlineData("ftp://files.com/webhook")]
    public void Constructor_InvalidEndpointUrl_ThrowsArgumentException(string invalidUrl)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new SupplierWebhook(ValidSupplierId, invalidUrl, true, ValidContract, ValidKvRef)
        );

        Assert.Equal("endpointUrl", ex.ParamName);
        Assert.Contains("Endpoint URL must be a valid HTTPS absolute URI", ex.Message);
    }

    [Fact]
    public void Disable_SetsIsEnabledToFalse()
    {
        // Arrange
        var webhook = new SupplierWebhook(
            ValidSupplierId,
            ValidEndpoint,
            true,
            ValidContract,
            ValidKvRef
        );

        // Act
        webhook.Disable();

        // Assert
        Assert.False(webhook.IsEnabled);
    }

    [Fact]
    public void Enable_SetsIsEnabledToTrue()
    {
        // Arrange
        var webhook = new SupplierWebhook(
            ValidSupplierId,
            ValidEndpoint,
            false,
            ValidContract,
            ValidKvRef
        );

        // Act
        webhook.Enable();

        // Assert
        Assert.True(webhook.IsEnabled);
    }
}
