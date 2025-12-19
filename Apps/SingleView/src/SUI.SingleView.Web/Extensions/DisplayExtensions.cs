using SUI.SingleView.Domain.Models;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Web.Extensions;

public static class DisplayExtensions
{
    public static string ToYesNo(this bool? value) =>
        value switch
        {
            true => "Yes",
            false => "No",
            _ => "",
        };

    public static Address ToAddress(this AddressV1? value) =>
        value == null
            ? new Address { AddressLine1 = "No known address" }
            : new Address
            {
                AddressLine1 = value.Line1 ?? "",
                AddressLine2 = value.Line2,
                Town = value.TownOrCity,
                County = value.County,
                Postcode = value.Postcode,
            };
}
