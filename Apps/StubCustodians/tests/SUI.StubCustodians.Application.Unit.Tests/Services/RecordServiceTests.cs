using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services
{
    public class RecordServiceTests
    {
        private readonly RecordService _recordService;
        private readonly IDataProvider _dataProvider;
        private readonly IConfiguration _configuration;

        public RecordServiceTests()
        {
            _dataProvider = Substitute.For<IDataProvider>();
            _configuration = Substitute.For<IConfiguration>();
            _recordService = new RecordService(_dataProvider, _configuration);
        }

        [Fact]
        public async Task GetRecordById_ShouldReturnRecord_WhenInputDataIsValid()
        {
            const string orgId = "local-authority-01";
            const string recordType = "childrens-services.details";
            const int version = 1;
            const string personId = "llvwLYMyw4gDCN-FblGIYA";
            const string recordId = "de-1234";
            const string schemaUri =
                "https://schemas.example.gov.uk/sui/childrens-services.details.v1.json";
            var recordPayload = new ChildrensServicesDetailsRecord { KeyWorker = "James Smith" };
            _dataProvider
                .GetRecordByIdAsync(orgId, recordId, Arg.Any<CancellationToken>())
                .Returns(
                    new CustodianRecord
                    {
                        RecordId = recordId,
                        Version = version,
                        PersonId = personId,
                        SchemaUri = schemaUri,
                        RecordType = recordType,
                        Payload = JsonSerializer.SerializeToElement(recordPayload),
                    }
                );
            var result = await _recordService.GetRecord<ChildrensServicesDetailsRecord>(
                recordId,
                orgId
            );

            Assert.NotNull(result);
            Assert.IsType<RecordEnvelope<ChildrensServicesDetailsRecord>>(result);

            Assert.Equal(new Uri(schemaUri), result.SchemaUri);
            Assert.Equal(recordType, result.RecordType);
            Assert.Equal(version, result.Version);
        }

        [Fact]
        public async Task GetRecordById_ShouldReturnNull_WhenOrgNotFound()
        {
            const string orgId = "local-authority-01";
            const string recordId = "de-1234";
            var result = await _recordService.GetRecord<ChildrensServicesDetailsRecord>(
                recordId,
                orgId
            );
            Assert.Null(result);
        }
    }
}
