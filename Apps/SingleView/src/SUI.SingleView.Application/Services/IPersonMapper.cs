using SUI.SingleView.Application.Models;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Application.Services;

public interface IPersonMapper
{
    PersonModel Map(string nhsNumber, ConformedData conformedData);
}
