using System.Diagnostics.CodeAnalysis;

namespace SUI.Transfer.Domain;

/// <summary>
/// Childrens Social Care - Referral Summary
/// </summary>
[ExcludeFromCodeCoverage(
    Justification = "CSC is a valid acronym, it means Childrens Social Care. Currently there is no way to add acronyms for this code analysis warning: https://github.com/SonarSource/sonar-dotnet/issues/2440"
)]
public record CSCReferralSummary(string Label, int Value);
