using Bogus;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Domain.UnitTests.Extensions;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Application.UnitTests.Services;

public class RecordServiceTests
{
    private readonly ILogger<RecordService> _mockLogger = Substitute.For<ILogger<RecordService>>();
    private readonly ITransferApi _mockTransferClient = Substitute.For<ITransferApi>();
    private readonly Faker _faker = new("en_GB");

    [Fact]
    public async Task GetRecordAsync_WithNhsNumber_ReturnsResults()
    {
        // Arrange
        var nhsNumber = _faker.GenerateNhsNumber().Value;
        _mockTransferClient
            .TransferAsync(nhsNumber, TestContext.Current.CancellationToken)
            .Returns(new TransferResult { Id = nhsNumber });
        var recordService = new RecordService(_mockTransferClient, _mockLogger);

        // Act
        var result = await recordService.GetRecordAsync(
            nhsNumber,
            TestContext.Current.CancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<PersonModel>();
    }

    [Fact]
    public async Task GetRecordAsync_WhenTransferExceptionThrown_LogsError()
    {
        // Arrange
        var nhsNumber = _faker.GenerateNhsNumber().Value;
        var expectedException = new InvalidOperationException();
        _mockTransferClient
            .TransferAsync(nhsNumber, TestContext.Current.CancellationToken)
            .Throws(expectedException);
        var recordService = new RecordService(_mockTransferClient, _mockLogger);

        // Act
        _ = await recordService.GetRecordAsync(nhsNumber, TestContext.Current.CancellationToken);

        // Assert
        var call = _mockLogger
            .ReceivedCalls()
            .FirstOrDefault(c =>
                c.GetMethodInfo().Name == "Log" && (LogLevel)c.GetArguments()[0]! == LogLevel.Error
            );

        call.ShouldNotBeNull();
        call.GetArguments()[2]!
            .ToString()
            .ShouldBe($"An error occurred when trying to get the record for {nhsNumber}");
        call.GetArguments()[3].ShouldBeOfType<InvalidOperationException>();
    }
}
