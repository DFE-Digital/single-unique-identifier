using SUI.FakeCustodians.Application.Contracts.SystmOne;
using SUI.FakeCustodians.Application.Mappers;

namespace SUI.FakeCustodians.Application.Unit.Tests.Mappers;

public class SystmOneRecordMapperTests
{
    private readonly SystmOneRecordMapper _mapper;

    public SystmOneRecordMapperTests()
    {
        _mapper = new SystmOneRecordMapper();
    }

    [Fact]
    public void Map_ShouldThrow_WhenSuiIsNullOrWhitespace()
    {
        var record = new SystmOneRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 1),
            NhsNumber = "1111111111",
            GpContactNumber = "07890789078",
            GpName = "Test GP",
            GpSurgery = "Test Surgery",
        };

        Assert.Throws<ArgumentException>(() => _mapper.Map(string.Empty, record));
        Assert.Throws<ArgumentNullException>(() => _mapper.Map(null!, record));
        Assert.Throws<ArgumentException>(() => _mapper.Map("   ", record));
    }

    [Fact]
    public void Map_ShouldThrow_WhenSourceRecordIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _mapper.Map("1234567890", null!));
    }

    [Fact]
    public void Map_ShouldMapPersonalData_FromBaseRecord()
    {
        var sui = "1234567890";

        var record = new SystmOneRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 1),
            NhsNumber = sui,
            GpContactNumber = "07890789078",
            GpName = "Test GP",
            GpSurgery = "Test Surgery",
        };

        var result = _mapper.Map(sui, record);

        Assert.NotNull(result);
        Assert.Equal(sui, result.Sui);
        Assert.NotNull(result.Data?.PersonalData);

        var personal = result.Data!.PersonalData!;
        Assert.Equal("John", personal.FirstName);
        Assert.Equal("Doe", personal.LastName);
        Assert.Equal(new DateTime(2010, 1, 1), personal.DateOfBirth);
        Assert.Equal(sui, personal.NhsNumber);
    }

    [Fact]
    public void Map_ShouldMapOtherData_SpecificToSystmOne()
    {
        var sui = "1234567890";

        var record = new SystmOneRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 1),
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
                new SystmOneMissedAppointment
                {
                    Date = new DateTime(2022, 5, 1),
                    Reason = "Test Reason2",
                    Location = "Fulham Hospital",
                },
            ],
        };

        var result = _mapper.Map(sui, record);

        var gp = result.Data?.GpData;
        Assert.NotNull(gp);
        Assert.Equal("07890789078", gp.GpContactNumber);
        Assert.Equal("Test GP", gp.GpName);
        Assert.Equal("Test Surgery", gp.GpSurgery);

        var missedAppointments = gp.MissedAppointmentReasons?.ToArray();
        Assert.NotNull(missedAppointments);
        Assert.Equal(2, gp.MissedAppointments);

        Assert.Equal("Test Reason", missedAppointments[0].Reason);
        Assert.Equal("Wiltshire Hospital", missedAppointments[0].Location);
        Assert.Equal("Test Reason2", missedAppointments[1].Reason);
        Assert.Equal("Fulham Hospital", missedAppointments[1].Location);
    }

    [Fact]
    public void Map_ShouldMapOtherData_SpecificToSystmOne_WhenMissedAppointmentsIsNull()
    {
        var sui = "1234567890";

        var record = new SystmOneRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 1),
            NhsNumber = sui,
            GpContactNumber = "07890789078",
            GpName = "Test GP",
            GpSurgery = "Test Surgery",
        };

        var result = _mapper.Map(sui, record);

        var gp = result.Data?.GpData;
        Assert.NotNull(gp);
        Assert.Equal("07890789078", gp.GpContactNumber);
        Assert.Equal("Test GP", gp.GpName);
        Assert.Equal("Test Surgery", gp.GpSurgery);

        Assert.Equal(0, gp.MissedAppointments);
    }
}
