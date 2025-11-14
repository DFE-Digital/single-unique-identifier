using SUI.FakeCustodians.Application.Contracts.Mosaic;
using SUI.FakeCustodians.Application.Mappers;

namespace SUI.FakeCustodians.Application.Unit.Tests.Mappers;

public class MosaicRecordMapperTests
{
    private readonly MosaicRecordMapper _mapper;

    public MosaicRecordMapperTests()
    {
        _mapper = new MosaicRecordMapper();
    }

    [Fact]
    public void Map_ShouldThrow_WhenSuiIsNullOrWhitespace()
    {
        var record = new MosaicRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 1),
            NhsNumber = "1111111111",
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

        var record = new MosaicRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 1),
            NhsNumber = sui,
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
    public void Map_ShouldMapOtherData_SpecificToMosaic()
    {
        var sui = "1234567890";

        var record = new MosaicRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 1),
            NhsNumber = sui,
            Referrals =
            [
                new MosaicReferral
                {
                    Id = "Ref1",
                    Date = new DateTime(2022, 1, 1),
                    Reason = "Test Reason",
                },
                new MosaicReferral
                {
                    Id = "Ref2",
                    Date = new DateTime(2022, 1, 2),
                    Reason = "Test Reason2",
                },
            ],
        };

        var result = _mapper.Map(sui, record);

        var cahms = result.Data?.CamhsData;
        Assert.NotNull(cahms);

        var referrals = cahms.Referrals?.ToArray();
        Assert.NotNull(referrals);
        Assert.Equal(2, referrals!.Length);

        Assert.Equal("Ref1", referrals[0].Id);
        Assert.Equal("Test Reason", referrals[0].Reason);
        Assert.Equal("Ref2", referrals[1].Id);
        Assert.Equal("Test Reason2", referrals[1].Reason);
    }
}
