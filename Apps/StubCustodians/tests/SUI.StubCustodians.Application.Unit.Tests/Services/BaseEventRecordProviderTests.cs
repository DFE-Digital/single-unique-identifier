using System.Text.Json;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Models;
using SUI.FakeCustodians.Application.Services;

namespace SUI.FakeCustodians.Application.Unit.Tests.Services
{
    public class BaseEventRecordProviderTests
    {
        private class DummyRecord
        {
            public string? Name { get; set; }
        }

        private class DummyMapper : IRecordMapper<DummyRecord>
        {
            public EventResponse Map(string sui, DummyRecord sourceRecord) =>
                new EventResponse { Sui = sui, Data = new() };
        }

        private class DummyProvider : BaseEventRecordProvider<DummyRecord>
        {
            public DummyProvider(string basePath)
                : base(new DummyMapper(), "Dummy", basePath) { }
        }

        private readonly string _tempDir;

        public BaseEventRecordProviderTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var provider = new DummyProvider(_tempDir);
            var result = provider.GetEventRecordForSui("missing");
            Assert.Null(result);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldThrow_WhenSuiIsInvalid()
        {
            var provider = new DummyProvider(_tempDir);

            Assert.Throws<ArgumentException>(() => provider.GetEventRecordForSui(""));
            Assert.Throws<ArgumentNullException>(() => provider.GetEventRecordForSui(null!));
            Assert.Throws<ArgumentException>(() => provider.GetEventRecordForSui("   "));
        }

        [Fact]
        public void GetEventRecordForSui_ShouldThrow_WhenJsonIsInvalid()
        {
            string sui = "test";
            string filePath = Path.Combine(_tempDir, $"{sui}.json");
            File.WriteAllText(filePath, "invalid json");

            var provider = new DummyProvider(_tempDir);
            Assert.Throws<JsonException>(() => provider.GetEventRecordForSui(sui));
        }
    }
}
