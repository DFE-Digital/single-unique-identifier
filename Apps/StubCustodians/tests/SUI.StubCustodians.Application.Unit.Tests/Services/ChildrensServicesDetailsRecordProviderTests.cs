using System.Text.Json;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services
{
    public class ChildrensServicesDetailsRecordProviderTests
    {
        private readonly string _tempDir;

        public ChildrensServicesDetailsRecordProviderTests()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _tempDir = root;
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void GetRecordForSui_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var provider = new ChildrensServicesDetailsRecordProvider(_tempDir);

            var result = provider.GetRecordForSui("1234567890", "MockCrimeDataProvider");

            Assert.Null(result);
        }

        [Fact]
        public void GetRecordForSui_ShouldThrow_WhenInputsAreInvalid()
        {
            var provider = new ChildrensServicesDetailsRecordProvider(_tempDir);

            Assert.Throws<ArgumentException>(() => provider.GetRecordForSui("", "X"));
            Assert.Throws<ArgumentException>(() => provider.GetRecordForSui("   ", "X"));
            Assert.Throws<ArgumentNullException>(() => provider.GetRecordForSui(null!, "X"));

            Assert.Throws<ArgumentException>(() => provider.GetRecordForSui("123", ""));
            Assert.Throws<ArgumentException>(() => provider.GetRecordForSui("123", "   "));
            Assert.Throws<ArgumentNullException>(() => provider.GetRecordForSui("123", null!));
        }

        [Fact]
        public void GetRecordForSui_ShouldThrow_WhenJsonIsInvalid()
        {
            string sui = "2222222222";
            string providerId = "MockSocialCareProvider";

            string providerFolder = Path.Combine(_tempDir, providerId);
            Directory.CreateDirectory(providerFolder);

            string filePath = Path.Combine(
                providerFolder,
                $"{sui}_ChildSocialCareDetailsRecordV1.json"
            );
            File.WriteAllText(filePath, "invalid json");

            var provider = new ChildrensServicesDetailsRecordProvider(_tempDir);

            Assert.Throws<JsonException>(() => provider.GetRecordForSui(sui, providerId));
        }

        [Fact]
        public void GetRecordForSui_ShouldReturnEnvelope_WhenJsonIsValid()
        {
            string sui = "1010101010";
            string providerId = "MockSocialCareProvider";

            string providerFolder = Path.Combine(_tempDir, providerId);
            Directory.CreateDirectory(providerFolder);

            var record = new ChildrensServicesDetailsRecordV1
            {
                KeyWorker = "Alex McDonald",
                TeamInvolvement = ["Disabled Kids Team", "SEND Kids Team"],
            };

            string filePath = Path.Combine(
                providerFolder,
                $"{sui}_ChildSocialCareDetailsRecordV1.json"
            );
            File.WriteAllText(filePath, JsonSerializer.Serialize(record));

            var provider = new ChildrensServicesDetailsRecordProvider(_tempDir);

            var result = provider.GetRecordForSui(sui, providerId);

            Assert.NotNull(result);
            Assert.Equal("Alex McDonald", result.Payload.KeyWorker);
            Assert.NotNull(result.Payload.TeamInvolvement);
            Assert.Equal(2, result.Payload.TeamInvolvement.Count);
            Assert.Equal(
                new Uri("https://schemas.example.gov.uk/sui/ChildrensServicesDetailsRecordV1.json"),
                result.SchemaUri
            );
        }
    }
}
