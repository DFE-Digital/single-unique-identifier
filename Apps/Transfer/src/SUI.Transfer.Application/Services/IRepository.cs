using SUI.Transfer.Application.Models.Custodians;

namespace SUI.Transfer.Application.Services;

public interface IRepository
{
    void AddOrUpdate(ConsolidatedData consolidatedData);
}
