using SUi.Find.Application.Models;
using SUI.Find.Domain.Enums;

namespace Sui.Find.Application.UnitTests.Models;

public class MatchResultTests
{
    [Fact]
    public void Create_ShouldReturnExpected_Match_MatchResult()
    {
        // Arr
        var score = 0.95m;
        var stage = "stage2";
        var nhsNumber = "123456789";

        // Act
        var result = MatchResult.Match(score, stage, nhsNumber);

        // Assert
        Assert.Equal(score, result.Score);
        Assert.Equal(stage, result.ProcessStage);
        Assert.Equal(nhsNumber, result.NhsNumber);
        Assert.Equal(MatchStatus.Match, result.MatchStatus);
    }

    [Fact]
    public void Create_ShouldReturnExpected_ManyMatch_MatchResult()
    {
        // Arr
        var stage = "stage2";

        // Act
        var result = MatchResult.ManyMatch(stage);

        // Assert
        Assert.Equal(stage, result.ProcessStage);
        Assert.Equal(MatchStatus.ManyMatch, result.MatchStatus);
    }

    [Fact]
    public void Create_ShouldReturnExpected_NoMatch_MatchResult()
    {
        // Act
        var result = MatchResult.NoMatch();

        // Assert
        Assert.Equal(MatchStatus.NoMatch, result.MatchStatus);
    }

    [Fact]
    public void Create_ShouldReturnExpected_PotentialMatch_MatchResult()
    {
        // Arr
        var score = 0.95m;
        var stage = "stage2";
        var nhsNumber = "123456789";

        // Act
        var result = MatchResult.PotentialMatch(score, stage, nhsNumber);

        // Assert
        Assert.Equal(score, result.Score);
        Assert.Equal(stage, result.ProcessStage);
        Assert.Equal(nhsNumber, result.NhsNumber);
        Assert.Equal(MatchStatus.PotentialMatch, result.MatchStatus);
    }

    [Theory]
    [MemberData(nameof(MatchResultComparisonData))]
    public void IsBetterThan_Returns_Correct_Score(MatchResult current, MatchResult? other, bool expectedResult)
    {
        // Act
        var actualResult = current.IsBetterThan(other);
        //Assert
        Assert.Equal(actualResult, expectedResult);
    }

    public static IEnumerable<object?[]> MatchResultComparisonData()
    {
        // test when other MatchResult  is null
        yield return [MatchResult.Match(3, "stage", "123456789"), null, true];

        // test when current MatchResult is better than the other MatchResult
        yield return
        [
            MatchResult.Match(3, "stage", "123456789"), MatchResult.PotentialMatch(3, "stages", "123456789"), true
        ];
        // test when current MatchResult is not better than other MatchResult
        yield return
        [
            MatchResult.PotentialMatch(3, "stage", "123456789"), MatchResult.Match(3, "stage", "123456789"), false
        ];
        // test when current error MatchResult is not better than a no match MatchResult
        yield return
        [
            MatchResult.Error("error result"), MatchResult.NoMatch(), false
        ];
        // test when current MatchResult is equal to other MatchResult & current score is higher than other score
        yield return
        [
            MatchResult.Match(1, "stage", "123456789"), MatchResult.Match(0.9m, "stage", "123456789"), true
        ];
        // test when current MatchResult is equal to other MatchResult & current score is lower than other score
        yield return
        [
            MatchResult.Match(0.8m, "stage", "123456789"), MatchResult.Match(0.9m, "stage", "123456789"), false
        ];
        // test when current MatchResult is equal to other MatchResult & current & other score are equal
        // Equal results and score is not better than
        yield return
        [
            MatchResult.Match(1, "stage", "123456789"), MatchResult.Match(1, "stage", "123456789"), false
        ];
        // test when current noMatch MatchResult & other noMatch MatchResult have null scores
        // there is no netter than result
        yield return [MatchResult.NoMatch(), MatchResult.NoMatch(), false];
    }
}