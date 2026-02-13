using UIHarness.Models;

namespace UIHarness.Interfaces;

public interface IFindAnId
{
    Task<string> EnrolAsync(PersonRecord person, CancellationToken cancellationToken);
}
