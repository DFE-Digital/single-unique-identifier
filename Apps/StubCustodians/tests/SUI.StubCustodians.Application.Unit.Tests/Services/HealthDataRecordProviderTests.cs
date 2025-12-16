using System.Text.Json;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services
{
    public class HealthDataRecordProviderTests
    {
        private readonly string _tempDir;

        public HealthDataRecordProviderTests()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _tempDir = root;
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void GetRecordForSui_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var provider = new HealthDataRecordProvider(_tempDir);

            var result = provider.GetRecordForSui("1234567890", "MockHealthCareProvider");

            Assert.Null(result);
        }

        [Fact]
        public void GetRecordForSui_ShouldThrow_WhenInputsAreInvalid()
        {
            var provider = new HealthDataRecordProvider(_tempDir);

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
            string providerId = "MockHealthCareProvider";

            string providerFolder = Path.Combine(_tempDir, providerId);
            Directory.CreateDirectory(providerFolder);

            string filePath = Path.Combine(providerFolder, $"{sui}_HealthDataRecordV1.json");
            File.WriteAllText(filePath, "invalid json");

            var provider = new HealthDataRecordProvider(_tempDir);

            Assert.Throws<JsonException>(() => provider.GetRecordForSui(sui, providerId));
        }

        [Fact]
        public void GetRecordForSui_ShouldReturnEnvelope_WhenJsonIsValid()
        {
            string sui = "1010101010";
            string providerId = "MockHealthCareProvider";

            string providerFolder = Path.Combine(_tempDir, providerId);
            Directory.CreateDirectory(providerFolder);

            var record = new HealthDataRecordV1()
            {
                RegisteredGPSurgery = "Whitby Surgery",
                GPTelephone = "07512399988",
                CAMHSContactDetails = ["07512399989"],
            };

            string filePath = Path.Combine(providerFolder, $"{sui}_HealthDataRecordV1.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(record));

            var provider = new HealthDataRecordProvider(_tempDir);

            var result = provider.GetRecordForSui(sui, providerId);

            Assert.NotNull(result);
            Assert.Equal("Whitby Surgery", result.Payload.RegisteredGPSurgery);
            Assert.Equal("07512399988", result.Payload.GPTelephone);
            Assert.NotNull(result.Payload.CAMHSContactDetails);
            Assert.Contains("07512399989", result.Payload.CAMHSContactDetails);
            Assert.Equal(
                new Uri("https://schemas.example.gov.uk/sui/HealthDataRecordV1.json"),
                result.SchemaUri
            );
        }
    }
}
