using NSubstitute;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services
{
    public class ManifestServiceTests
    {
        private readonly ManifestService _manifestService;
        private readonly IDataProvider _dataProvider;

        public ManifestServiceTests()
        {
            _dataProvider = Substitute.For<IDataProvider>();
            _manifestService = new ManifestService(_dataProvider);
        }

        [Theory]
        [InlineData("local-authority-01")]
        [InlineData("health-01")]
        [InlineData("police-01")]
        [InlineData("test-01")]
        public async Task GetManifest_ShouldReturnProblem_WhenCancellationTokenIsCancelled(
            string orgId
        )
        {
            // const string orgId = "local-authority-01";
            const string recordType = "childrens-services.details";
            const int version = 1;
            const string personId = "llvwLYMyw4gDCN-FblGIYA";
            const string baseUrl = "https://localhost:5001";
            const string recordId = "de-1234";
            const string schemaUri =
                "https://schemas.example.gov.uk/sui/childrens-services.details.v1.json";

            _dataProvider
                .GetRecordsAsync(orgId, personId, CancellationToken.None)
                .Returns(
                    new List<CustodianRecord>
                    {
                        new()
                        {
                            RecordId = recordId,
                            Version = version,
                            PersonId = personId,
                            SchemaUri = schemaUri,
                            RecordType = recordType,
                        },
                    }
                );
            var result = await _manifestService.GetManifestForOrganisation(
                orgId,
                personId,
                baseUrl,
                string.Empty,
                CancellationToken.None
            );

            Assert.NotNull(result);
            Assert.IsType<List<SearchResultItem>>(result);
            var searchResultItem = result.First();
            Assert.Equal(orgId, searchResultItem.SystemId);
            Assert.Equal(recordId, searchResultItem.RecordId);
            Assert.Equal(recordType, searchResultItem.RecordType);
            if (orgId == "local-authority-01" || orgId == "health-01")
            {
                Assert.Equal(
                    $"{baseUrl}/api/v1/fetch/{Uri.EscapeDataString(orgId.ToLowerInvariant())}/{Uri.EscapeDataString(recordId)}?recordType={Uri.EscapeDataString(recordType)}",
                    searchResultItem.RecordUrl
                );
            }
            else
            {
                Assert.Equal(
                    $"{baseUrl}/api/v1/fetch/{Uri.EscapeDataString(orgId.ToLowerInvariant())}/{Uri.EscapeDataString(recordType)}/{Uri.EscapeDataString(recordId)}",
                    searchResultItem.RecordUrl
                );
            }
        }
    }
}
