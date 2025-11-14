using SUI.FakeCustodians.Application.Contracts;
using SUI.FakeCustodians.Application.Mappers;
using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Unit.Tests.Mappers
{
    public class BaseRecordMapperTests
    {
        private class TestRecord : BaseEntity { }

        private class TestRecordMapper : BaseRecordMapper<TestRecord>
        {
            public override EventResponse Map(string sui, TestRecord record)
            {
                return new EventResponse { Sui = sui };
            }

            public PersonalData InvokeMapToPersonalData(TestRecord record) =>
                MapToPersonalData(record);
        }

        [Fact]
        public void MapToPersonalData_ShouldMapBasicFieldsCorrectly()
        {
            var record = new TestRecord
            {
                FirstName = "Jane",
                LastName = "Doe",
                DateOfBirth = new DateTime(2000, 1, 1),
                NhsNumber = "1234567890",
            };
            var mapper = new TestRecordMapper();

            var result = mapper.InvokeMapToPersonalData(record);

            Assert.Equal("Jane", result.FirstName);
            Assert.Equal("Doe", result.LastName);
            Assert.Equal(new DateTime(2000, 1, 1), result.DateOfBirth);
            Assert.Equal("1234567890", result.NhsNumber);
        }

        [Fact]
        public void MapToPersonalData_ShouldThrow_WhenSourceIsNull()
        {
            var mapper = new TestRecordMapper();
            Assert.Throws<ArgumentNullException>(() => mapper.InvokeMapToPersonalData(null!));
        }
    }
}
