using SUI.UIHarness.Web.Models;

namespace SUI.UIHarness.Web.Services;

public interface IGetAnIdentifierService
{
    Task<PersonMatch> GetAnId(LocalPerson person, string clientId);
}
