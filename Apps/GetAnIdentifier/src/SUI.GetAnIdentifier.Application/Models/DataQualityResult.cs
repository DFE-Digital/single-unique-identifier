using SUI.GetAnIdentifier.Application.Enum;

namespace SUI.GetAnIdentifier.Application.Models;

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
