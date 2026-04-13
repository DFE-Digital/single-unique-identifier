using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.API.Controllers;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API.Unit.Tests
{
    public class FetchControllerTests
    {
        private readonly FetchController _fetchController;
        private readonly IRecordService _recordService;

        public FetchControllerTests()
        {
            var logger = Substitute.For<ILogger<FetchController>>();
            _recordService = Substitute.For<IRecordService>();
            _fetchController = new FetchController(logger, _recordService);
        }

        [Fact]
        public async Task GetRecord_ShouldReturnChildrensServicesRecord_WhenInputsAreCorrect()
        {
            const string orgId = "local-authority-01";
            const string recordType = "childrens-services.details";
            const string recordId = "CSC-5919-A";
            const string personId = "Cy13hyZL-4LSIwVy50p-Hg";
            const int version = 1;
            var record = new RecordEnvelope<ChildrensServicesDetailsRecord>
            {
                RecordId = recordId,
                PersonId = personId,
                RecordType = recordType,
                Version = version,
                ContactDetails =
                [
                    new ContactDetail
                    {
                        Name = "John Doe",
                        Description = "John Doe",
                        Email = "test@test.com",
                        Address = "1 test street",
                        Telephone = "0123456789",
                    },
                ],
                RecordLinks =
                [
                    new RecordLink { Title = "Test Link", Url = "https://www.example.gov.uk/" },
                ],
                SchemaUri = new Uri(
                    "https://schemas.example.gov.uk/sui/childrens-services.details.v1.json"
                ),
                Payload = new ChildrensServicesDetailsRecord { KeyWorker = "Alex Patel" },
            };
            _recordService
                .GetRecord<ChildrensServicesDetailsRecord>(recordId, orgId)
                .Returns(record);

            var actionResult = await _fetchController.GetRecordEndpoint1(
                orgId,
                recordType,
                recordId
            );

            Assert.IsType<OkObjectResult>(actionResult);
            var result = actionResult as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.IsType<RecordEnvelope<ChildrensServicesDetailsRecord>>(result.Value);

            var childrensServicesRecord =
                result.Value as RecordEnvelope<ChildrensServicesDetailsRecord>;

            Assert.NotNull(childrensServicesRecord);
            Assert.NotNull(childrensServicesRecord.RecordLinks);
            Assert.NotNull(childrensServicesRecord.ContactDetails);
            Assert.Equal(record.SchemaUri, childrensServicesRecord.SchemaUri);
            Assert.Equal(record.RecordLinks[0].Title, childrensServicesRecord.RecordLinks[0].Title);
            Assert.Equal(record.RecordLinks[0].Url, childrensServicesRecord.RecordLinks[0].Url);
            Assert.Equal(
                record.ContactDetails[0].Name,
                childrensServicesRecord.ContactDetails[0].Name
            );
            Assert.Equal(
                record.ContactDetails[0].Address,
                childrensServicesRecord.ContactDetails[0].Address
            );
            Assert.Equal(
                record.ContactDetails[0].Description,
                childrensServicesRecord.ContactDetails[0].Description
            );
            Assert.Equal(
                record.ContactDetails[0].Email,
                childrensServicesRecord.ContactDetails[0].Email
            );
            Assert.Equal(
                record.ContactDetails[0].Telephone,
                childrensServicesRecord.ContactDetails[0].Telephone
            );
            Assert.Equal(record.Payload.KeyWorker, childrensServicesRecord.Payload.KeyWorker);
        }

        [Fact]
        public async Task GetRecord_ShouldReturnEducationDetailsRecord_WhenInputsAreCorrect()
        {
            const string orgId = "education-01";
            const string recordType = "education.details";
            const string recordId = "ATT-5919-1";
            const string personId = "Cy13hyZL-4LSIwVy50p-Hg";
            const int version = 1;
            var record = new RecordEnvelope<EducationDetailsRecord>
            {
                PersonId = personId,
                RecordId = recordId,
                RecordType = recordType,
                Version = version,
                ContactDetails =
                [
                    new ContactDetail
                    {
                        Name = "John Doe",
                        Description = "John Doe",
                        Email = "test@test.com",
                        Address = "1 test street",
                        Telephone = "0123456789",
                    },
                ],
                RecordLinks =
                [
                    new RecordLink { Title = "Test Link", Url = "https://www.example.gov.uk/" },
                ],
                SchemaUri = new Uri(
                    "https://schemas.example.gov.uk/sui/childrens-services.details.v1.json"
                ),
                Payload = new EducationDetailsRecord { EducationSettingName = "ST Johns" },
            };
            _recordService.GetRecord<EducationDetailsRecord>(recordId, orgId).Returns(record);

            var actionResult = await _fetchController.GetRecordEndpoint1(
                orgId,
                recordType,
                recordId
            );

            Assert.IsType<OkObjectResult>(actionResult);
            var result = actionResult as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.IsType<RecordEnvelope<EducationDetailsRecord>>(result.Value);

            var educationRecord = result.Value as RecordEnvelope<EducationDetailsRecord>;
            Assert.NotNull(educationRecord);
            Assert.Equal(record.SchemaUri, educationRecord.SchemaUri);
            Assert.Equal(
                record.Payload.EducationSettingName,
                educationRecord.Payload.EducationSettingName
            );
        }

        [Fact]
        public async Task GetRecord_ShouldReturnCrimeRecord_WhenInputsAreCorrect()
        {
            const string orgId = "police-01";
            const string recordType = "crime-justice.details";
            const string recordId = "ATT-5919-1";
            const string personId = "Cy13hyZL-4LSIwVy50p-Hg";
            const int version = 1;
            var record = new RecordEnvelope<CrimeDataRecord>
            {
                PersonId = personId,
                RecordId = recordId,
                RecordType = recordType,
                Version = version,
                ContactDetails =
                [
                    new ContactDetail
                    {
                        Name = "John Doe",
                        Description = "John Doe",
                        Email = "test@test.com",
                        Address = "1 test street",
                        Telephone = "0123456789",
                    },
                ],
                RecordLinks =
                [
                    new RecordLink { Title = "Test Link", Url = "https://www.example.gov.uk/" },
                ],
                SchemaUri = new Uri(
                    "https://schemas.example.gov.uk/sui/childrens-services.details.v1.json"
                ),
                Payload = new CrimeDataRecord
                {
                    PoliceMarkerDetails =
                        "Individuals at the address may resort to violent behaviour",
                },
            };
            _recordService.GetRecord<CrimeDataRecord>(recordId, orgId).Returns(record);

            var actionResult = await _fetchController.GetRecordEndpoint1(
                orgId,
                recordType,
                recordId
            );

            Assert.IsType<OkObjectResult>(actionResult);
            var result = actionResult as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.IsType<RecordEnvelope<CrimeDataRecord>>(result.Value);

            var crimeRecord = result.Value as RecordEnvelope<CrimeDataRecord>;
            Assert.NotNull(crimeRecord);
            Assert.Equal(record.SchemaUri, crimeRecord.SchemaUri);
            Assert.Equal(
                record.Payload.PoliceMarkerDetails,
                crimeRecord.Payload.PoliceMarkerDetails
            );
        }

        [Fact]
        public async Task GetRecord_ShouldReturnHealthRecord_WhenInputsAreCorrect()
        {
            const string orgId = "health-01";
            const string recordType = "health.details";
            const string recordId = "ATT-5919-1";
            const string personId = "Cy13hyZL-4LSIwVy50p-Hg";
            const int version = 1;
            var record = new RecordEnvelope<HealthDataRecord>
            {
                PersonId = personId,
                RecordId = recordId,
                RecordType = recordType,
                Version = version,
                ContactDetails =
                [
                    new ContactDetail
                    {
                        Name = "John Doe",
                        Description = "John Doe",
                        Email = "test@test.com",
                        Address = "1 test street",
                        Telephone = "0123456789",
                    },
                ],
                RecordLinks =
                [
                    new RecordLink { Title = "Test Link", Url = "https://www.example.gov.uk/" },
                ],
                SchemaUri = new Uri(
                    "https://schemas.example.gov.uk/sui/childrens-services.details.v1.json"
                ),
                Payload = new HealthDataRecord { RegisteredGPSurgery = "Duke Medical Centre" },
            };
            _recordService.GetRecord<HealthDataRecord>(recordId, orgId).Returns(record);

            var actionResult = await _fetchController.GetRecordEndpoint1(
                orgId,
                recordType,
                recordId
            );

            Assert.IsType<OkObjectResult>(actionResult);
            var result = actionResult as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.IsType<RecordEnvelope<HealthDataRecord>>(result.Value);

            var healthRecord = result.Value as RecordEnvelope<HealthDataRecord>;
            Assert.NotNull(healthRecord);
            Assert.Equal(record.SchemaUri, healthRecord.SchemaUri);
            Assert.Equal(
                record.Payload.RegisteredGPSurgery,
                healthRecord.Payload.RegisteredGPSurgery
            );
        }

        [Fact]
        public async Task GetRecord_ShouldReturnPersonalDetailsRecord_WhenInputsAreCorrect()
        {
            const string orgId = "local-authority-01";
            const string recordType = "personal.details";
            const string recordId = "ATT-5919-1";
            const string personId = "Cy13hyZL-4LSIwVy50p-Hg";
            const int version = 1;
            var record = new RecordEnvelope<PersonalDetailsRecord>
            {
                PersonId = personId,
                RecordId = recordId,
                RecordType = recordType,
                Version = version,
                ContactDetails =
                [
                    new ContactDetail
                    {
                        Name = "John Doe",
                        Description = "John Doe",
                        Email = "test@test.com",
                        Address = "1 test street",
                        Telephone = "0123456789",
                    },
                ],
                RecordLinks =
                [
                    new RecordLink { Title = "Test Link", Url = "https://www.example.gov.uk/" },
                ],
                SchemaUri = new Uri(
                    "https://schemas.example.gov.uk/sui/childrens-services.details.v1.json"
                ),
                Payload = new PersonalDetailsRecord { FirstName = "James" },
            };
            _recordService.GetRecord<PersonalDetailsRecord>(recordId, orgId).Returns(record);

            var actionResult = await _fetchController.GetRecordEndpoint2(
                orgId,
                recordId,
                recordType
            );

            Assert.IsType<OkObjectResult>(actionResult);
            var result = actionResult as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.IsType<RecordEnvelope<PersonalDetailsRecord>>(result.Value);

            var personalDetailsRecord = result.Value as RecordEnvelope<PersonalDetailsRecord>;
            Assert.NotNull(personalDetailsRecord);
            Assert.Equal(record.RecordType, personalDetailsRecord.RecordType);
            Assert.Equal(record.Version, personalDetailsRecord.Version);
            Assert.Equal(record.SchemaUri, personalDetailsRecord.SchemaUri);
            Assert.Equal(record.Payload.FirstName, personalDetailsRecord.Payload.FirstName);
        }

        [Fact]
        public async Task GetRecord_ShouldReturnNotFound_WhenInputsAreIncorrect()
        {
            const string orgId = "local-authority-01";
            const string recordType = "childrens-services.details";
            const string recordId = "CSC-5919-A";
            const string personId = "Cy13hyZL-4LSIwVy50p-Hg";
            const int version = 1;
            var record = new RecordEnvelope<ChildrensServicesDetailsRecord>
            {
                PersonId = personId,
                RecordId = recordId,
                RecordType = recordType,
                Version = version,
                ContactDetails =
                [
                    new ContactDetail
                    {
                        Name = "John Doe",
                        Description = "John Doe",
                        Email = "test@test.com",
                        Address = "1 test street",
                        Telephone = "0123456789",
                    },
                ],
                RecordLinks =
                [
                    new RecordLink { Title = "Test Link", Url = "https://www.example.gov.uk/" },
                ],
                SchemaUri = new Uri(
                    "https://schemas.example.gov.uk/sui/childrens-services.details.v1.json"
                ),
                Payload = new ChildrensServicesDetailsRecord { KeyWorker = "Alex Patel" },
            };
            _recordService
                .GetRecord<ChildrensServicesDetailsRecord>(recordId, orgId)
                .Returns(record);

            var actionResult = await _fetchController.GetRecordEndpoint1(orgId, recordType, "TEST");

            Assert.IsType<NotFoundResult>(actionResult);
            var result = actionResult as NotFoundResult;
            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }
    }
}
