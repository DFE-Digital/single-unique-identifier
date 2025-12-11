using System.Text.Json;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services
{
    public class BaseRecordProviderTests
    {
        private class DummyRecord
        {
            public string? Name { get; set; }
        }

        private class DummyProvider : BaseRecordProvider<DummyRecord>
        {
            public DummyProvider(string basePath)
                : base(basePath) { }
        }

        private readonly string _tempDir;

        public BaseRecordProviderTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void GetRecordForSui_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var provider = new DummyProvider(_tempDir);

            var result = provider.GetRecordForSui("1234567890", "DummyProvider");

            Assert.Null(result);
        }

        [Fact]
        public void GetRecordForSui_ShouldThrow_WhenSuiIsInvalid()
        {
            var provider = new DummyProvider(_tempDir);

            Assert.Throws<ArgumentException>(() => provider.GetRecordForSui("", "X"));
            Assert.Throws<ArgumentException>(() => provider.GetRecordForSui("   ", "X"));
            Assert.Throws<ArgumentNullException>(() => provider.GetRecordForSui(null!, "X"));
        }

        [Fact]
        public void GetRecordForSui_ShouldThrow_WhenProviderSystemIdIsInvalid()
        {
            var provider = new DummyProvider(_tempDir);

            Assert.Throws<ArgumentException>(() => provider.GetRecordForSui("1234567890", ""));
            Assert.Throws<ArgumentException>(() => provider.GetRecordForSui("1234567890", "   "));
            Assert.Throws<ArgumentNullException>(() =>
                provider.GetRecordForSui("1234567890", null!)
            );
        }

        [Fact]
        public void GetRecordForSui_ShouldThrow_WhenJsonIsInvalid()
        {
            string sui = "1234567890";
            string providerSystemId = "DummyProvider";

            string providerFolder = Path.Combine(_tempDir, providerSystemId);
            Directory.CreateDirectory(providerFolder);

            string filePath = Path.Combine(providerFolder, $"{sui}_DummyRecord.json");
            File.WriteAllText(filePath, "invalid json");

            var provider = new DummyProvider(_tempDir);

            Assert.Throws<JsonException>(() => provider.GetRecordForSui(sui, providerSystemId));
        }

        [Fact]
        public void GetRecordForSui_ShouldReturnRecordEnvelope_WhenJsonIsValid()
        {
            string sui = "9999999999";
            string providerSystemId = "DummyProvider";

            string providerFolder = Path.Combine(_tempDir, providerSystemId);
            Directory.CreateDirectory(providerFolder);

            var record = new DummyRecord { Name = "John" };

            string filePath = Path.Combine(providerFolder, $"{sui}_DummyRecord.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(record));

            var provider = new DummyProvider(_tempDir);

            var result = provider.GetRecordForSui(sui, providerSystemId);

            Assert.NotNull(result);
            Assert.Equal(
                new Uri("https://schemas.example.gov.uk/sui/DummyRecord.json"),
                result.SchemaUri
            );
            Assert.Equal("John", result.Payload.Name);
        }
    }
}
