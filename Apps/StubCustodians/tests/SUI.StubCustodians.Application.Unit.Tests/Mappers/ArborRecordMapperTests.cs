using SUI.StubCustodians.Application.Contracts.Arbor;
using SUI.StubCustodians.Application.Mappers;

namespace SUI.StubCustodians.Application.Unit.Tests.Mappers
{
    public class ArborRecordMapperTests
    {
        private readonly ArborRecordMapper _mapper;

        public ArborRecordMapperTests()
        {
            _mapper = new ArborRecordMapper();
        }

        [Fact]
        public void Map_ShouldThrow_WhenSuiIsNullOrWhitespace()
        {
            var record = new ArborRecord
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

            var record = new ArborRecord
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
        public void Map_ShouldMapOtherData_SpecificToArbor()
        {
            var sui = "1234567890";

            var record = new ArborRecord
            {
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(2010, 1, 1),
                NhsNumber = sui,
                PupilPremium = true,
                FreeSchoolMeals = false,
                ElectivelyHomeEducated = true,
                SchoolsAttended =
                [
                    new ArborSchool { Name = "Test School 1", Address = "123 Street" },
                    new ArborSchool { Name = "Test School 2", Address = "456 Avenue" },
                ],
            };

            var result = _mapper.Map(sui, record);

            var education = result.Data?.EducationData;
            Assert.NotNull(education);
            Assert.True(education!.PupilPremium);
            Assert.False(education.FreeSchoolMeals);
            Assert.True(education.ElectivelyHomeEducated);

            var schools = education.SchoolsAttended?.ToArray();
            Assert.NotNull(schools);
            Assert.Equal(2, schools!.Length);

            Assert.Equal("Test School 1", schools[0].Name);
            Assert.Equal("123 Street", schools[0].Address);
            Assert.Equal("Test School 2", schools[1].Name);
            Assert.Equal("456 Avenue", schools[1].Address);
        }
    }
}
