using Bogus;
using Shouldly;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Domain.UnitTests.Extensions;

namespace SUI.SingleView.Application.UnitTests.Services;

public class RecordServiceTests
{
    private readonly RecordService _recordService = new();
    private readonly Faker _faker = new("en_GB");

    [Fact]
    public void Search_WithNhsNumber_ReturnsResults()
    {
        // Arrange
        var nhsNumber = _faker.GenerateNhsNumber().Value;

        // Act
        var result = _recordService.GetRecord(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<PersonModel>();
    }
}
