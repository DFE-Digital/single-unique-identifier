using System.Text.Json;
using NSubstitute;
using SUI.StubCustodians.Application.Contracts.Mosaic;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services
{
    public class MosaicEventRecordProviderTests
    {
        private readonly IRecordMapper<MosaicRecord> _mapper;
        private readonly string _tempDir;

        public MosaicEventRecordProviderTests()
        {
            _mapper = Substitute.For<IRecordMapper<MosaicRecord>>();

            // Create isolated temp folder: <temp>/<guid>/Mosaic/
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _tempDir = Path.Combine(root, "Mosaic");
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var provider = new MosaicEventRecordProvider(_mapper, _tempDir);
            var result = provider.GetEventRecordForSui("0000000000");
            Assert.Null(result);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldCallMapper_WhenFileExists()
        {
            string sui = "testSUI";

            string filePath = Path.Combine(_tempDir, $"{sui}.json");

            var sampleRecord = new MosaicRecord()
            {
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1986, 4, 26),
                NhsNumber = sui,
                Referrals =
                [
                    new MosaicReferral
                    {
                        Id = "1",
                        Date = new DateTime(2022, 1, 1),
                        Reason = "Test reason",
                    },
                ],
            };

            // Write JSON to temp folder
            File.WriteAllText(filePath, JsonSerializer.Serialize(sampleRecord));

            // Setup mapper stub
            _mapper.Map(sui, Arg.Any<MosaicRecord>()).Returns(new EventResponse { Sui = sui });

            var provider = new MosaicEventRecordProvider(_mapper, _tempDir);

            var result = provider.GetEventRecordForSui(sui);

            Assert.NotNull(result);
            Assert.Equal(sui, result.Sui);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldThrow_WhenSuiIsInvalid()
        {
            var provider = new MosaicEventRecordProvider(_mapper, _tempDir);

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

            var provider = new MosaicEventRecordProvider(_mapper, _tempDir);

            Assert.Throws<JsonException>(() => provider.GetEventRecordForSui(sui));
        }
    }
}
