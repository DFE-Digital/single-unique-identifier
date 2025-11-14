using System.Text.Json;
using NSubstitute;
using SUI.FakeCustodians.Application.Contracts.SystmOne;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Models;
using SUI.FakeCustodians.Application.Services;

namespace SUI.FakeCustodians.Application.Unit.Tests.Services;

public class SystmOneEventRecordProviderTests
{
    private readonly IRecordMapper<SystmOneRecord> _mapper;
    private readonly string _tempDir;

    public SystmOneEventRecordProviderTests()
    {
        _mapper = Substitute.For<IRecordMapper<SystmOneRecord>>();

        // Create isolated temp folder: <temp>/<guid>/SystmOne/
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _tempDir = Path.Combine(root, "SystmOne");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void GetEventRecordForSui_ShouldReturnNull_WhenFileDoesNotExist()
    {
        var provider = new SystmOneEventRecordProvider(_mapper, _tempDir);
        var result = provider.GetEventRecordForSui("0000000000");
        Assert.Null(result);
    }

    [Fact]
    public void GetEventRecordForSui_ShouldCallMapper_WhenFileExists()
    {
        string sui = "testSUI";

        string filePath = Path.Combine(_tempDir, $"{sui}.json");

        var sampleRecord = new SystmOneRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1986, 4, 26),
            NhsNumber = sui,
            GpContactNumber = "07890789078",
            GpName = "Test GP",
            GpSurgery = "Test Surgery",
            MissedAppointmentReasons =
            [
                new SystmOneMissedAppointment
                {
                    Date = new DateTime(2022, 1, 1),
                    Reason = "Test Reason",
                    Location = "Wiltshire Hospital",
                },
            ],
        };

        // Write JSON to temp folder
        File.WriteAllText(filePath, JsonSerializer.Serialize(sampleRecord));

        // Setup mapper stub
        _mapper.Map(sui, Arg.Any<SystmOneRecord>()).Returns(new EventResponse { Sui = sui });

        var provider = new SystmOneEventRecordProvider(_mapper, _tempDir);

        var result = provider.GetEventRecordForSui(sui);

        Assert.NotNull(result);
        Assert.Equal(sui, result.Sui);
    }

    [Fact]
    public void GetEventRecordForSui_ShouldThrow_WhenSuiIsInvalid()
    {
        var provider = new SystmOneEventRecordProvider(_mapper, _tempDir);

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

        var provider = new SystmOneEventRecordProvider(_mapper, _tempDir);

        Assert.Throws<JsonException>(() => provider.GetEventRecordForSui(sui));
    }
}
