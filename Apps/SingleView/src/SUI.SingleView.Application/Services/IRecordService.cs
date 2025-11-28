using SUI.SingleView.Application.Models;

namespace SUI.SingleView.Application.Services;

public interface IRecordService
{
    Task<PersonModel> GetRecordAsync(string nhsNumber);
}
