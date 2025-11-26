using Bogus;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Domain.UnitTests.Extensions;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Application.UnitTests.Services;

public class RecordServiceTests
{
    private readonly ILogger<RecordService> _mockLogger = Substitute.For<ILogger<RecordService>>();
    private readonly ITransferApi _mockTransferClient = Substitute.For<ITransferApi>();
    private readonly Faker _faker = new("en_GB");

    [Fact]
    public async Task Search_WithNhsNumber_ReturnsResults()
    {
        // Arrange
        var nhsNumber = _faker.GenerateNhsNumber().Value;
        _mockTransferClient
            .TransferAsync(nhsNumber, TestContext.Current.CancellationToken)
            .Returns(new TransferResult { Id = nhsNumber });
        var recordService = new RecordService(_mockTransferClient, _mockLogger);

        // Act
        var result = await recordService.GetRecord(nhsNumber);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<PersonModel>();
    }
}
