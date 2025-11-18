using SUI.SingleView.Application.Models;

namespace SUI.SingleView.Application.Services;

public interface IRecordService
{
    PersonModel GetRecord(string nhsNumber);
}
