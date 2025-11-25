using System.IO.Abstractions;
using NSubstitute;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class AuthStoreServiceTests
{
    private readonly AuthStoreService _sut;
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();

    public AuthStoreServiceTests()
    {
        _sut = new AuthStoreService(_mockFileSystem);
    }

    [Fact]
    public async Task GetAuthStore_ShouldReturnAuthStoreWithPopulatedFields_WhenFileIsValid()
    {
        // Arrange
        var realFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "auth-clients.json");
        var fileContent = await File.ReadAllTextAsync(realFilePath);
        _mockFileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(fileContent);

        // Act
        var authStore = await _sut.GetAuthStore();

        // Assert
        Assert.NotNull(authStore);
        Assert.Equal("https://sandbox.api.example.gov.uk/find-a-record/auth", authStore.Issuer);
        Assert.Equal("find-a-record-api", authStore.Audience);
        Assert.Equal(
            "Wc8Kq1ZyR4hNfD0uVx3mS9JpA6eLrT2bG7wQvY5sCjP8kF1nH0tUoMzBiXaEdRl",
            authStore.SigningKey
        );
        Assert.NotNull(authStore.Clients);
        Assert.NotEmpty(authStore.Clients);
    }
}
