using SUI.FakeCustodians.Application.Contracts.Arbor;
using SUI.FakeCustodians.Application.Mappers;

namespace SUI.FakeCustodians.Application.Unit.Tests
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
                NhsNumber = "1111111111"
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
        public void Map_ShouldMapPersonalDataCorrectly()
        {
            var sui = "1234567890";
            var record = new ArborRecord
            {
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(2010, 1, 1),
                NhsNumber = sui
            };

            var result = _mapper.Map(sui, record);

            Assert.NotNull(result);
            Assert.Equal(sui, result.Sui);
            Assert.NotNull(result.Data?.PersonalData);
            Assert.Equal("John", result.Data.PersonalData.FirstName);
            Assert.Equal("Doe", result.Data.PersonalData.LastName);
            Assert.Equal(new DateTime(2010, 1, 1), result.Data.PersonalData.DateOfBirth);
            Assert.Equal(sui, result.Data.PersonalData.NhsNumber);
        }

        [Fact]
        public void Map_ShouldMapEducationDataCorrectly()
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
                    new ArborSchool { Name = "Test School 2", Address = "456 Avenue" }
                ]
            };

            var result = _mapper.Map(sui, record);

            Assert.NotNull(result.Data?.EducationData);
            Assert.True(result.Data.EducationData.PupilPremium);
            Assert.False(result.Data.EducationData.FreeSchoolMeals);
            Assert.True(result.Data.EducationData.ElectivelyHomeEducated);

            var schools = result.Data.EducationData.SchoolsAttended?.ToArray();
            
            Assert.NotNull(schools);
            Assert.Equal(2, schools.Length);
            Assert.Equal("Test School 1", schools[0].Name);
            Assert.Equal("123 Street", schools[0].Address);
            Assert.Equal("Test School 2", schools[1].Name);
            Assert.Equal("456 Avenue", schools[1].Address);
        }
    }
}
