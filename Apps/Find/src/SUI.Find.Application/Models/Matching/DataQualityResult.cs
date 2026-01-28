using SUI.Find.Application.Enums.Matching;

namespace SUI.Find.Application.Models.Matching;

public class DataQualityResult
{
    public QualityType Given { get; set; } = QualityType.Valid;

    public QualityType Family { get; set; } = QualityType.Valid;

    public QualityType BirthDate { get; set; } = QualityType.Valid;

    public QualityType AddressPostalCode { get; set; } = QualityType.Valid;

    public QualityType Phone { get; set; } = QualityType.Valid;

    public QualityType Email { get; set; } = QualityType.Valid;

    public QualityType Gender { get; set; } = QualityType.Valid;
}
