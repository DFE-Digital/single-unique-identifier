using System.Text.Json;
using NSubstitute;
using SUI.StubCustodians.Application.Contracts.Niche;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services
{
    public class NicheEventRecordProviderTests
    {
        private readonly IRecordMapper<NicheRecord> _mapper;
        private readonly string _tempDir;

        public NicheEventRecordProviderTests()
        {
            _mapper = Substitute.For<IRecordMapper<NicheRecord>>();

            // Create isolated temp folder: <temp>/<guid>/Niche/
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _tempDir = Path.Combine(root, "Niche");
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var provider = new NicheEventRecordProvider(_mapper, _tempDir);
            var result = provider.GetEventRecordForSui("0000000000");
            Assert.Null(result);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldCallMapper_WhenFileExists()
        {
            string sui = "testSUI";

            string filePath = Path.Combine(_tempDir, $"{sui}.json");

            var sampleRecord = new NicheRecord()
            {
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1986, 4, 26),
                NhsNumber = sui,
                ChildProtection = true,
                KnownToPolice = false,
                PolicePowersOfProtection = true,
            };

            // Write JSON to temp folder
            File.WriteAllText(filePath, JsonSerializer.Serialize(sampleRecord));

            // Setup mapper stub
            _mapper.Map(sui, Arg.Any<NicheRecord>()).Returns(new EventResponse { Sui = sui });

            var provider = new NicheEventRecordProvider(_mapper, _tempDir);

            var result = provider.GetEventRecordForSui(sui);

            Assert.NotNull(result);
            Assert.Equal(sui, result.Sui);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldThrow_WhenSuiIsInvalid()
        {
            var provider = new NicheEventRecordProvider(_mapper, _tempDir);

            Assert.Throws<ArgumentException>(() => provider.GetEventRecordForSui(""));
            Assert.Throws<ArgumentException>(() => provider.GetEventRecordForSui("   "));
            Assert.Throws<ArgumentNullException>(() => provider.GetEventRecordForSui(null!));
        }

        [Fact]
        public void GetEventRecordForSui_ShouldThrow_WhenJsonIsInvalid()
        {
            string sui = "2222222222";
            string filePath = Path.Combine(_tempDir, $"{sui}.json");

            // Write invalid JSON
            File.WriteAllText(filePath, "invalid json content");

            var provider = new NicheEventRecordProvider(_mapper, _tempDir);

            Assert.Throws<JsonException>(() => provider.GetEventRecordForSui(sui));
        }
    }
}
