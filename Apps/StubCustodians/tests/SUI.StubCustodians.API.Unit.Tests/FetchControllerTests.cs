using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
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
        // private readonly HttpClient _client;

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
            var orgId = "local-authority-01";
            var recordType = "childrens-services.details";
            var recordId = "CSC-5919-A";
            var record = new RecordEnvelope<ChildrensServicesDetailsRecord>
            {
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
            Assert.Equal(record.SchemaUri, childrensServicesRecord.SchemaUri);
            Assert.Equal(record.Payload.KeyWorker, childrensServicesRecord.Payload.KeyWorker);
        }

        [Fact]
        public async Task GetRecord_ShouldReturnEducationDetailsRecord_WhenInputsAreCorrect()
        {
            var orgId = "education-01";
            var recordType = "education.details";
            var recordId = "ATT-5919-1";
            var record = new RecordEnvelope<EducationDetailsRecord>
            {
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
            var orgId = "police-01";
            var recordType = "crime-justice.details";
            var recordId = "ATT-5919-1";
            var record = new RecordEnvelope<CrimeDataRecord>
            {
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
            var orgId = "health-01";
            var recordType = "health.details";
            var recordId = "ATT-5919-1";
            var record = new RecordEnvelope<HealthDataRecord>
            {
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
            var orgId = "local-authority-01";
            var recordType = "personal.details";
            var recordId = "ATT-5919-1";
            var record = new RecordEnvelope<PersonalDetailsRecord>
            {
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
            Assert.Equal(record.SchemaUri, personalDetailsRecord.SchemaUri);
            Assert.Equal(record.Payload.FirstName, personalDetailsRecord.Payload.FirstName);
        }

        [Fact]
        public async Task GetRecord_ShouldReturnNotFound_WhenInputsAreIncorrect()
        {
            var orgId = "local-authority-01";
            var recordType = "childrens-services.details";
            var recordId = "CSC-5919-A";
            var record = new RecordEnvelope<ChildrensServicesDetailsRecord>
            {
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
