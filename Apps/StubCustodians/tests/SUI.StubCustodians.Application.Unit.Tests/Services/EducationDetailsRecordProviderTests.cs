using System.Text.Json;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services
{
    public class EducationDetailsRecordProviderTests
    {
        private readonly string _tempDir;

        public EducationDetailsRecordProviderTests()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _tempDir = root;
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void GetRecordForSui_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var provider = new EducationDetailsRecordProvider(_tempDir);

            var result = provider.GetRecordForSui("1234567890", "MockCrimeDataProvider");

            Assert.Null(result);
        }

        [Fact]
        public void GetRecordForSui_ShouldThrow_WhenInputsAreInvalid()
        {
            var provider = new EducationDetailsRecordProvider(_tempDir);

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
            string providerId = "MockEducationProvider";

            string providerFolder = Path.Combine(_tempDir, providerId);
            Directory.CreateDirectory(providerFolder);

            string filePath = Path.Combine(providerFolder, $"{sui}_EducationDetailsRecordV1.json");
            File.WriteAllText(filePath, "invalid json");

            var provider = new EducationDetailsRecordProvider(_tempDir);

            Assert.Throws<JsonException>(() => provider.GetRecordForSui(sui, providerId));
        }

        [Fact]
        public void GetRecordForSui_ShouldReturnEnvelope_WhenJsonIsValid()
        {
            string sui = "1010101010";
            string providerId = "MockEducationProvider";

            string providerFolder = Path.Combine(_tempDir, providerId);
            Directory.CreateDirectory(providerFolder);

            var record = new EducationDetailsRecord()
            {
                EducationSettingName = "TestAcademy",
                EducationSettingTelephone = "01234567890",
            };

            string filePath = Path.Combine(providerFolder, $"{sui}_EducationDetailsRecordV1.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(record));

            var provider = new EducationDetailsRecordProvider(_tempDir);

            var result = provider.GetRecordForSui(sui, providerId);

            Assert.NotNull(result);
            Assert.Equal("TestAcademy", result.Payload.EducationSettingName);
            Assert.Equal("01234567890", result.Payload.EducationSettingTelephone);
            Assert.Equal(
                new Uri("https://schemas.example.gov.uk/sui/EducationDetailsRecordV1.json"),
                result.SchemaUri
            );
        }
    }
}
