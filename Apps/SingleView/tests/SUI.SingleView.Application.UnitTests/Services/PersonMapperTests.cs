using FluentAssertions;
using Shouldly;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Application.UnitTests.Services;

public class PersonMapperTests
{
    private static readonly PersonMapper Sut = new();

    [Theory]
    [InlineData("Dave", "Gorman", "Dave Gorman")]
    [InlineData("", "Gorman", "Gorman")]
    [InlineData("Dave", null, "Dave")]
    [InlineData(null, null, "Unknown name")]
    [InlineData("", "", "Unknown name")]
    [InlineData(null, "", "Unknown name")]
    [InlineData("", null, "Unknown name")]
    [InlineData(" ", " \t ", "Unknown name")]
    public void Map_DoesMap_Name_AsExpected(
        string? firstName,
        string? lastName,
        string expectedResult
    )
    {
        var input = new ConformedData
        {
            ConsolidatedData = new ConsolidatedData
            {
                PersonalDetailsRecord = new PersonalDetailsRecordV1Consolidated
                {
                    FirstName = new ConsolidatedFieldOfstring { Value = firstName },
                    LastName = new ConsolidatedFieldOfstring { Value = lastName },
                },
            },
        };

        // ACT
        var result = Sut.Map("", input);

        // ASSERT
        result.Name.ShouldBe(expectedResult);
    }

    [Fact]
    public void Map_Does_Gracefully_Map()
    {
        var expectedResult = new PersonModel { Name = "Unknown name" };

        // ACT
        var result = Sut.Map("", new ConformedData());

        // ASSERT
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void Map_DoesMap_NhsNumber()
    {
        // ACT
        var result = Sut.Map("9449303223", new ConformedData());

        // ASSERT
        result.NhsNumber.ShouldBe("9449303223");
    }

    [Fact]
    public void Map_Does_PullAcross_RecordData()
    {
        var input = new ConformedData
        {
            ConsolidatedData = new ConsolidatedData
            {
                PersonalDetailsRecord = new PersonalDetailsRecordV1Consolidated(),
                ChildrensServicesDetailsRecord = new ChildrensServicesDetailsRecordV1Consolidated(),
                EducationDetailsRecord = new EducationDetailsRecordV1Consolidated(),
                HealthDataRecord = new HealthDataRecordV1Consolidated(),
                CrimeDataRecord = new CrimeDataRecordV1Consolidated(),
            },
        };

        // ACT
        var result = Sut.Map("", input);

        // ASSERT
        result.PersonalDetails.ShouldBeSameAs(input.ConsolidatedData.PersonalDetailsRecord);
        result.ChildrensServicesDetails.ShouldBeSameAs(
            input.ConsolidatedData.ChildrensServicesDetailsRecord
        );
        result.EducationDetails.ShouldBeSameAs(input.ConsolidatedData.EducationDetailsRecord);
        result.HealthData.ShouldBeSameAs(input.ConsolidatedData.HealthDataRecord);
        result.CrimeData.ShouldBeSameAs(input.ConsolidatedData.CrimeDataRecord);
    }

    [Fact]
    public void Map_DoesMap_Tags_AsExpected()
    {
        var input = new ConformedData
        {
            StatusFlags =
            [
                StatusFlag.IsChildInNeed,
                StatusFlag.HasPupilPremium,
                StatusFlag.IsOpenToCAMHS,
                StatusFlag.RiskOfExploitationCriminal,
            ],
        };

        // ACT
        var result = Sut.Map("", input);

        // ASSERT
        result.Tags.ShouldBe([
            "Is Child In Need",
            "Has Pupil Premium",
            "Is Open To CAMHS",
            "Risk Of Exploitation Criminal",
        ]);
    }

    [Theory]
    [InlineData(
        "Individuals at the address may resort to violent behaviour",
        true,
        "Individuals at the address may resort to violent behaviour"
    )]
    [InlineData(null, false, "")]
    [InlineData("", false, "")]
    [InlineData("  \t  ", false, "")]
    public void Map_DoesMap_PoliceMarker_AsExpected(
        string? policeMarkerDetails,
        bool expectedIsPoliceMarker,
        string expectedPoliceMarkerDetails
    )
    {
        var input = new ConformedData
        {
            ConsolidatedData = new ConsolidatedData
            {
                CrimeDataRecord = new CrimeDataRecordV1Consolidated
                {
                    PoliceMarkerDetails = new ConsolidatedFieldOfstring
                    {
                        Value = policeMarkerDetails,
                    },
                },
            },
        };

        // ACT
        var result = Sut.Map("", input);

        // ASSERT
        result.PoliceMarker.ShouldBe(expectedIsPoliceMarker);
        result.PoliceMarkerDetails.ShouldBe(expectedPoliceMarkerDetails);
    }

    [Fact]
    public void Map_Does_PullAcross_ConformedSummaries()
    {
        var input = new ConformedData
        {
            ChildServicesReferralSummaries = new ChildServicesReferralSummaries(),
            EducationAttendanceSummaries = new EducationAttendanceSummaries(),
            HealthAttendanceSummaries = new HealthAttendanceSummaries(),
        };

        // ACT
        var result = Sut.Map("", input);

        // ASSERT
        result.ChildServicesReferralSummaries.ShouldBeSameAs(input.ChildServicesReferralSummaries);
        result.EducationAttendanceSummaries.ShouldBeSameAs(input.EducationAttendanceSummaries);
        result.HealthAttendanceSummaries.ShouldBeSameAs(input.HealthAttendanceSummaries);
    }

    [Fact]
    public void Map_DoesMap_MissingEpisodesLast6Months_AsExpected()
    {
        // ACT
        var result = Sut.Map(
            "",
            new ConformedData
            {
                CrimeMissingEpisodesSummaries = new CrimeMissingEpisodesSummaries
                {
                    Last6Months = new Array[12],
                },
            }
        );

        // ASSERT
        result.MissingEpisodesLast6Months.ShouldBe(12);
    }

    [Fact]
    public void Map_DoesMap_MissingEpisodesLast6Months_AsNull_WhenNoData()
    {
        // ACT
        var result = Sut.Map("", new ConformedData { CrimeMissingEpisodesSummaries = null });

        // ASSERT
        result.MissingEpisodesLast6Months.ShouldBeNull();
    }
}
