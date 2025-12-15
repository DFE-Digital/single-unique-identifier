using Microsoft.Extensions.Time.Testing;
using SUI.Custodians.API.Client;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;
using SUI.Transfer.Domain.Services;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class MissingEpisodesTransformerTests
{
    private readonly DateTimeOffset _dateTimeOffset = new(new DateTime(2025, 12, 01));

    [Fact]
    public void ApplyTransformation_WithNoCrimeDetails_ShouldReturnNull()
    {
        var sut = new MissingEpisodesTransformer(new FakeTimeProvider(_dateTimeOffset));

        // ACT
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                PersonalDetailsRecord = null,
                ChildrensServicesDetailsRecord = null,
                EducationDetailsRecord = null,
                HealthDataRecord = null,
                CrimeDataRecord = null,
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // ASSERT
        Assert.Null(result);
    }

    [Fact]
    public void ApplyTransformation_WithNullMissingEpisodes_ShouldReturnNull()
    {
        var sut = new MissingEpisodesTransformer(new FakeTimeProvider(_dateTimeOffset));

        // ACT
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                PersonalDetailsRecord = null,
                ChildrensServicesDetailsRecord = null,
                EducationDetailsRecord = null,
                HealthDataRecord = null,
                CrimeDataRecord = new CrimeDataRecordV1Consolidated
                {
                    MissingEpisodes = (List<CrimeMissingEpisodeV1>?)null,
                },
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // ASSERT
        Assert.Null(result);
    }

    [Fact]
    public void ApplyTransformation_WithNoMissingEpisodes_ShouldReturnNull()
    {
        var sut = new MissingEpisodesTransformer(new FakeTimeProvider(_dateTimeOffset));

        // ACT
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                PersonalDetailsRecord = null,
                ChildrensServicesDetailsRecord = null,
                EducationDetailsRecord = null,
                HealthDataRecord = null,
                CrimeDataRecord = new CrimeDataRecordV1Consolidated
                {
                    MissingEpisodes = new List<CrimeMissingEpisodeV1>(),
                },
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // ASSERT
        Assert.Null(result);
    }

    [Fact]
    public void ApplyTransformation_WithNoMissingEpisodesInLast6Months_ShouldReturnEmptyList()
    {
        var sut = new MissingEpisodesTransformer(new FakeTimeProvider(_dateTimeOffset));

        // ACT
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                PersonalDetailsRecord = null,
                ChildrensServicesDetailsRecord = null,
                EducationDetailsRecord = null,
                HealthDataRecord = null,
                CrimeDataRecord = new CrimeDataRecordV1Consolidated
                {
                    MissingEpisodes = new List<CrimeMissingEpisodeV1>
                    {
                        new() { Date = _dateTimeOffset.AddMonths(-8) },
                        new() { Date = _dateTimeOffset.AddMonths(-12) },
                    },
                },
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // ASSERT
        Assert.NotNull(result);
        Assert.NotNull(result.Last6Months);
        Assert.Empty(result.Last6Months);
    }

    [Fact]
    public void ApplyTransformation_WithMissingEpisodesInLast6Months_ShouldReturnFilteredResults()
    {
        var sut = new MissingEpisodesTransformer(new FakeTimeProvider(_dateTimeOffset));

        // ACT
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                PersonalDetailsRecord = null,
                ChildrensServicesDetailsRecord = null,
                EducationDetailsRecord = null,
                HealthDataRecord = null,
                CrimeDataRecord = new CrimeDataRecordV1Consolidated
                {
                    MissingEpisodes = new List<CrimeMissingEpisodeV1>
                    {
                        new() { Date = _dateTimeOffset.AddMonths(-2) },
                        new() { Date = _dateTimeOffset.AddMonths(-3) },
                        new() { Date = _dateTimeOffset.AddMonths(-4) },
                        new() { Date = _dateTimeOffset.AddMonths(-6) },
                        new() { Date = _dateTimeOffset.AddMonths(-8) },
                    },
                },
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // ASSERT
        Assert.NotNull(result);
        Assert.NotNull(result.Last6Months);
        Assert.Equal(4, result.Last6Months.Count);
        Assert.Equivalent(
            new List<CrimeMissingEpisodeV1>
            {
                new() { Date = _dateTimeOffset.AddMonths(-2) },
                new() { Date = _dateTimeOffset.AddMonths(-3) },
                new() { Date = _dateTimeOffset.AddMonths(-4) },
                new() { Date = _dateTimeOffset.AddMonths(-6) },
            },
            result.Last6Months,
            true
        );
    }
}
