using Bogus;
using Bogus.Extensions.UnitedKingdom;
using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Domain.UnitTests.Fakers;

public sealed class AddressFaker : Faker<Address>
{
    public AddressFaker(string locale = "en_GB")
        : base(locale)
    {
        StrictMode(true);

        RuleFor(x => x.AddressLine1, f => f.Address.StreetAddress());
        RuleFor(x => x.AddressLine2, f => f.Address.SecondaryAddress().OrNull(f, 0.75f));
        RuleFor(x => x.Town, f => f.Address.City());
        RuleFor(x => x.County, f => f.Address.County());
        RuleFor(x => x.Country, f => f.Address.CountryOfUnitedKingdom());
        RuleFor(x => x.Postcode, f => f.Address.ZipCode());
    }
}
