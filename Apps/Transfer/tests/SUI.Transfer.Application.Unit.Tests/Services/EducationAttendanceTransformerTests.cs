using Microsoft.Extensions.Time.Testing;
using SUI.Custodians.API.Client;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class EducationAttendanceTransformerTests
{
    private readonly DateTimeOffset _octoberDateTimeOffset = new(new DateTime(2025, 10, 01));
    private readonly DateTimeOffset _aprilDateTimeOffset = new(new DateTime(2025, 04, 01));

    [Fact]
    public void ApplyTransformation_WhenNoEducationDetails_ShouldReturnNull()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_octoberDateTimeOffset);

        var sut = new EducationAttendanceTransformer(fakeTimeProvider);

        // Act
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                ChildPersonalDetailsRecord = null,
                ChildSocialCareDetailsRecord = null,
                EducationDetailsRecord = null,
                ChildHealthDataRecord = null,
                ChildLinkedCrimeDataRecord = null,
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ApplyTransformation_WhenNoAttendanceDetails_ShouldReturnNull()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_octoberDateTimeOffset);

        var sut = new EducationAttendanceTransformer(fakeTimeProvider);

        // Act
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                ChildPersonalDetailsRecord = null,
                ChildSocialCareDetailsRecord = null,
                EducationDetailsRecord = new EducationDetailsRecordV1
                {
                    EducationAttendances = null,
                },
                ChildHealthDataRecord = null,
                ChildLinkedCrimeDataRecord = null,
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ApplyTransformation_WithNoAttendancesInCurrentOrPreviousYear_ShouldReturnResultsWithNullProperties()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_octoberDateTimeOffset);

        var attendanceRecord = new EducationAttendanceV1
        {
            AcademicTermYearStart = 2020,
            AcademicTermYearEnd = 2021,
            Exclusions = 1,
        };

        var sut = new EducationAttendanceTransformer(fakeTimeProvider);

        // Act
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                ChildPersonalDetailsRecord = null,
                ChildSocialCareDetailsRecord = null,
                EducationDetailsRecord = new EducationDetailsRecordV1
                {
                    EducationAttendances = new List<EducationAttendanceV1> { attendanceRecord },
                },
                ChildHealthDataRecord = null,
                ChildLinkedCrimeDataRecord = null,
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.CurrentAcademicYear);
        Assert.Null(result.LastAcademicYear);
    }

    [Fact]
    public void ApplyTransformation_October_WithAttendances_ShouldReturnResults()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_octoberDateTimeOffset);

        var currentAttendanceRecord = new EducationAttendanceV1
        {
            AcademicTermYearStart = 2025,
            AcademicTermYearEnd = 2026,
            Exclusions = 0,
            Suspensions = 2,
            UnauthorisedAbsencePercentage = 0.05f,
        };

        var previousAttendanceRecord = new EducationAttendanceV1
        {
            AcademicTermYearStart = 2024,
            AcademicTermYearEnd = 2025,
            Exclusions = 1,
            Suspensions = 5,
            UnauthorisedAbsencePercentage = 0.01f,
        };

        var sut = new EducationAttendanceTransformer(fakeTimeProvider);

        // Act
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                ChildPersonalDetailsRecord = null,
                ChildSocialCareDetailsRecord = null,
                EducationDetailsRecord = new EducationDetailsRecordV1
                {
                    EducationAttendances = new List<EducationAttendanceV1>
                    {
                        currentAttendanceRecord,
                        previousAttendanceRecord,
                    },
                },
                ChildHealthDataRecord = null,
                ChildLinkedCrimeDataRecord = null,
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CurrentAcademicYear);
        Assert.Equal(currentAttendanceRecord, result.CurrentAcademicYear);
        Assert.NotNull(result.LastAcademicYear);
        Assert.Equal(previousAttendanceRecord, result.LastAcademicYear);
    }

    [Fact]
    public void ApplyTransformation_April_WithAttendances_ShouldReturnResults()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_aprilDateTimeOffset);

        var currentAttendanceRecord = new EducationAttendanceV1
        {
            AcademicTermYearStart = 2024,
            AcademicTermYearEnd = 2025,
            Exclusions = 0,
            Suspensions = 2,
            UnauthorisedAbsencePercentage = 0.05f,
        };

        var previousAttendanceRecord = new EducationAttendanceV1
        {
            AcademicTermYearStart = 2023,
            AcademicTermYearEnd = 2024,
            Exclusions = 1,
            Suspensions = 5,
            UnauthorisedAbsencePercentage = 0.01f,
        };

        var sut = new EducationAttendanceTransformer(fakeTimeProvider);

        // Act
        var result = sut.ApplyTransformation(
            new ConsolidatedData("999-000-1234")
            {
                ChildPersonalDetailsRecord = null,
                ChildSocialCareDetailsRecord = null,
                EducationDetailsRecord = new EducationDetailsRecordV1
                {
                    EducationAttendances = new List<EducationAttendanceV1>
                    {
                        currentAttendanceRecord,
                        previousAttendanceRecord,
                    },
                },
                ChildHealthDataRecord = null,
                ChildLinkedCrimeDataRecord = null,
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CurrentAcademicYear);
        Assert.Equal(currentAttendanceRecord, result.CurrentAcademicYear);
        Assert.NotNull(result.LastAcademicYear);
        Assert.Equal(previousAttendanceRecord, result.LastAcademicYear);
    }
}
