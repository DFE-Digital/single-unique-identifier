using Bogus;
using SUI.SingleView.Domain.Models;
using SUI.SingleView.Domain.UnitTests.Extensions;

namespace SUI.SingleView.Domain.UnitTests.Fakers;

public sealed class SearchResultFaker : Faker<SearchResult>
{
    public SearchResultFaker(string locale = "en_GB")
        : base(locale)
    {
        StrictMode(true);

        var addressFaker = new AddressFaker(locale);

        RuleFor(x => x.NhsNumber, f => f.GenerateNhsNumber());
        RuleFor(x => x.Name, f => f.Person.FullName);
        RuleFor(x => x.DateOfBirth, f => f.Date.Past(18));
        RuleFor(x => x.Address, _ => addressFaker.Generate());
    }

    public SearchResultFaker WithNhsNumber(NhsNumber nhsNumber)
    {
        RuleFor(x => x.NhsNumber, _ => nhsNumber);
        return this;
    }

    public SearchResultFaker WithName(string name)
    {
        RuleFor(x => x.Name, _ => name);
        return this;
    }

    public SearchResultFaker WithAddress(Address address)
    {
        RuleFor(x => x.Address, _ => address);
        return this;
    }

    public SearchResultFaker WithDateOfBirth(DateTime dateOfBirth)
    {
        RuleFor(x => x.DateOfBirth, _ => dateOfBirth);
        return this;
    }
}
