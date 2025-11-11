using System.Text.Json;
using NSubstitute;
using SUI.FakeCustodians.Application.Contracts.Arbor;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Models;
using SUI.FakeCustodians.Application.Services;

namespace SUI.FakeCustodians.Application.Unit.Tests
{
    public class ArborEventRecordProviderTests
    {
        private readonly IRecordMapper<ArborRecord> _mapper;
        private readonly string _tempDir;

        public ArborEventRecordProviderTests()
        {
            _mapper = Substitute.For<IRecordMapper<ArborRecord>>();

            // Create isolated temp folder: <temp>/<guid>/Arbor/
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _tempDir = Path.Combine(root, "Arbor");
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var provider = new ArborEventRecordProvider(_mapper, _tempDir);
            string sui = "0000000000";

            var result = provider.GetEventRecordForSui(sui);

            Assert.Null(result);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldCallMapper_WhenFileExists()
        {
            string sui = "testSUI";
            string filePath = Path.Combine(_tempDir, $"{sui}.json");

            var sampleRecord = new ArborRecord
            {
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1986, 4, 26),
                NhsNumber = sui,
                PupilPremium = true,
                FreeSchoolMeals = true,
                ElectivelyHomeEducated = false,
                SchoolsAttended =
                [
                    new ArborSchool
                    {
                        Name = "Wiltshire School",
                        Address = "111 Wiltshire Street, London, SW1 1AA",
                    },
                ],
            };

            File.WriteAllText(filePath, JsonSerializer.Serialize(sampleRecord));

            _mapper.Map(sui, Arg.Any<ArborRecord>()).Returns(new EventResponse { Sui = sui });

            var provider = new ArborEventRecordProvider(_mapper, _tempDir);

            var result = provider.GetEventRecordForSui(sui);

            Assert.NotNull(result);
            Assert.Equal(sui, result.Sui);
        }

        [Fact]
        public void GetEventRecordForSui_ShouldThrow_WhenSuiIsInvalid()
        {
            var provider = new ArborEventRecordProvider(_mapper, _tempDir);

            Assert.Throws<ArgumentException>(() => provider.GetEventRecordForSui(""));
            Assert.Throws<ArgumentException>(() => provider.GetEventRecordForSui("   "));
            Assert.Throws<ArgumentNullException>(() => provider.GetEventRecordForSui(null!));
        }

        [Fact]
        public void GetEventRecordForSui_ShouldThrow_WhenJsonIsInvalid()
        {
            string sui = "2222222222";
            string filePath = Path.Combine(_tempDir, $"{sui}.json");

            File.WriteAllText(filePath, "invalid json content");

            var provider = new ArborEventRecordProvider(_mapper, _tempDir);

            Assert.Throws<JsonException>(() => provider.GetEventRecordForSui(sui));
        }
    }
}
