using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IStatusFlagsTransformer
{
    StatusFlag[]? ApplyTransformation(ConsolidatedData consolidatedData);
}
