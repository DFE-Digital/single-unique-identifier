using SUI.FakeCustodians.Application.Contracts.Niche;
using SUI.FakeCustodians.Application.Mappers;

namespace SUI.FakeCustodians.Application.Unit.Tests.Mappers;

public class NicheRecordMapperTests
{
    private readonly NicheRecordMapper _mapper;

    public NicheRecordMapperTests()
    {
        _mapper = new NicheRecordMapper();
    }

    [Fact]
    public void Map_ShouldThrow_WhenSuiIsNullOrWhitespace()
    {
        var record = new NicheRecord()
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
        // Arrange
        var sui = "1234567890";
        var record = new NicheRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 1),
            NhsNumber = sui,
        };

        // Act
        var result = _mapper.Map(sui, record);

        // Assert
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
    public void Map_ShouldMapOtherData_SpecificToNiche()
    {
        // Arrange
        var sui = "1234567890";
        var record = new NicheRecord()
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(2010, 1, 1),
            NhsNumber = sui,
            ChildProtection = true,
            KnownToPolice = false,
            PolicePowersOfProtection = true,
        };

        // Act
        var result = _mapper.Map(sui, record);

        // Assert
        var police = result.Data?.PoliceData;
        Assert.NotNull(police);
        Assert.True(police!.ChildProtection);
        Assert.False(police.KnownToPolice);
        Assert.True(police.PolicePowersOfProtection);
    }
}
