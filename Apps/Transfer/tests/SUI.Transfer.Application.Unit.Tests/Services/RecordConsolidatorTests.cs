using NSubstitute;
using SUI.Custodians.API.Client;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;
using SUI.Transfer.Domain.Services;
using SUI.Transfer.Domain.SourceGenerated;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class RecordConsolidatorTests
{
    [Fact]
    public void ConsolidateRecords_Does_Invoke_Consolidation_AsExpected()
    {
        var mockConsolidateRecordCollectionsService =
            Substitute.For<IConsolidateRecordCollectionsService>();
        var mockConsolidationFieldRanker = Substitute.For<IConsolidationFieldRanker>();

        var sut = new RecordConsolidator(
            mockConsolidateRecordCollectionsService,
            mockConsolidationFieldRanker
        );

        var unconsolidatedData = new UnconsolidatedData("XXX 000 1234")
        {
            PersonalDetailsRecords = new IProviderRecord<PersonalDetailsRecordV1>[1],
            ChildrensServicesDetailsRecords = new IProviderRecord<ChildSocialCareDetailsRecordV1>[
                2
            ],
            EducationDetailsRecords = new IProviderRecord<EducationDetailsRecordV1>[1],
            HealthDataRecords = new IProviderRecord<HealthDataRecordV1>[2],
            CrimeDataRecords = new IProviderRecord<CrimeDataRecordV1>[1],
            FailedFetches = new FailedFetch[3],
        };

        // ACT
        var result = sut.ConsolidateRecords(unconsolidatedData);

        // ASSERT
        result.Sui.Should().Be("XXX 000 1234");
        result.CountOfRecordsSuccessfullyFetched.Should().BeGreaterThan(0);
        result
            .CountOfRecordsSuccessfullyFetched.Should()
            .Be(unconsolidatedData.CountOfRecordsSuccessfullyFetched);
        result.FailedFetches.Should().BeSameAs(unconsolidatedData.FailedFetches);

        mockConsolidateRecordCollectionsService
            .Received()
            .ConsolidateRecords(
                unconsolidatedData.PersonalDetailsRecords,
                mockConsolidationFieldRanker.RankField
            );

        mockConsolidateRecordCollectionsService
            .Received()
            .ConsolidateRecords(
                unconsolidatedData.ChildrensServicesDetailsRecords,
                mockConsolidationFieldRanker.RankField
            );

        mockConsolidateRecordCollectionsService
            .Received()
            .ConsolidateRecords(
                unconsolidatedData.EducationDetailsRecords,
                mockConsolidationFieldRanker.RankField
            );

        mockConsolidateRecordCollectionsService
            .Received()
            .ConsolidateRecords(
                unconsolidatedData.HealthDataRecords,
                mockConsolidationFieldRanker.RankField
            );

        mockConsolidateRecordCollectionsService
            .Received()
            .ConsolidateRecords(
                unconsolidatedData.CrimeDataRecords,
                mockConsolidationFieldRanker.RankField
            );
    }
}
