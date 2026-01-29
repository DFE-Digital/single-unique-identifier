using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.FindApi.Functions.ActivityFunctions;
using SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class QueryProvidersFunctionTests
{
    private readonly ILogger<QueryProvidersFunction> _mockLogger = Substitute.For<
        ILogger<QueryProvidersFunction>
    >();
    private readonly IQueryProvidersService _mockQueryProvidersService =
        Substitute.For<IQueryProvidersService>();
    private readonly IIdRegisterRepository _mockIdRegisterRepository =
        Substitute.For<IIdRegisterRepository>();
    private readonly FunctionContext _mockContext = Substitute.For<FunctionContext>();
    private readonly QueryProvidersFunction _sut;

    public QueryProvidersFunctionTests()
    {
        _sut = new QueryProvidersFunction(
            _mockLogger,
            _mockQueryProvidersService,
            _mockIdRegisterRepository
        );
    }

    [Fact]
    public async Task QueryProvider_CallsService_AndReturnsResult()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            new ProviderDefinition { OrgId = "org1" }
        );

        var expectedItems = new List<SearchResultItem>
        {
            new("SystemA", "Provider A", "Type1", "/v1/records/original-id"),
        };
        var expectedResult = Result<IReadOnlyList<SearchResultItem>>.Ok(expectedItems);

        _mockQueryProvidersService
            .QueryProvidersAsync(input, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _sut.QueryProvider(_mockContext, input, CancellationToken.None);

        // Assert
        await _mockQueryProvidersService
            .Received(1)
            .QueryProvidersAsync(input, Arg.Any<CancellationToken>());

        await _mockIdRegisterRepository
            .Received(expectedItems.Count)
            .UpsertAsync(
                Arg.Is<IdRegisterEntry>(e =>
                    e.Sui == input.Suid
                    && e.CustodianId == expectedItems[0].ProviderId
                    && e.SystemId == expectedItems[0].ProviderSystem
                    && e.RecordType == expectedItems[0].RecordType
                    && e.Provenance == Provenance.DiscoveredViaFanout
                ),
                Arg.Any<CancellationToken>()
            );

        Assert.Single(result);
        Assert.Equal(expectedItems, result);
    }
}
