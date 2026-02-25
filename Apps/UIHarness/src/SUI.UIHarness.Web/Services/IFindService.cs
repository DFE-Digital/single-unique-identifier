using SUI.UIHarness.Web.Models;
using SUI.UIHarness.Web.Models.Find;

namespace SUI.UIHarness.Web.Services;

public interface IFindService
{
    Task<FindMatchResult> MatchRecord(LocalPerson person, string clientId);

    Task<string> StartSearch(string clientId, string suid);

    Task<FindSearchResults> FindRecords(string clientId, string jobId);

    Task<FindCustodianRecord> FetchRecord(string clientId, string recordId);
}
