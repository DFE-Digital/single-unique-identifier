using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Mapping;
using SUI.Find.Infrastructure.Constants;

namespace SUI.Find.Infrastructure.Services;

public class SearchIdService : ISearchIdService
{
    private static string FormatGender(string? inputGender)
    {
        string gender;
        if (string.IsNullOrWhiteSpace(inputGender))
        {
            gender = "";
        }
        else if (int.TryParse(inputGender, out _))
        {
            gender = GenderMapper.FromDbsGenderCode(inputGender).Value;
        }
        else
        {
            gender = inputGender;
        }
        return gender;
    }
    private static string FormatPostalCode(string? inputPostalCode)
    {
        if (string.IsNullOrWhiteSpace(inputPostalCode))
        {
            return "";
        }
        return new string(inputPostalCode
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray())
            .ToLowerInvariant();
    }

    private static string FormatName(string? name) => string.IsNullOrWhiteSpace(name) ? "" : name!.ToLowerInvariant();
    private static string FormatBirthDate(DateOnly? birthDate) => birthDate is { } date ? date.ToString("dd/MM/yyyy") : "";

    private static SearchIdHash CreateHash(string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);
        var hashBytes = SHA256.HashData(bytes);

        var builder = new StringBuilder();
        foreach (var byt in hashBytes)
        {
            builder.Append(byt.ToString("x2"));
        }

        return new SearchIdHash(builder.ToString());
    }
    private static string PrepareDataString(string given, string family, string birthDate, string gender, string postalCode)
    {
        return $"{given}{family}{birthDate}{gender}{postalCode}";
    }

    public SearchIdHash CreatePersonHash(string? given, string? family,DateOnly? birthDate, string? gender, string? addressPostalCode)
    {
        var formattedGiven = FormatName(given);
        var formattedFamily = FormatName(family);
        var formattedGender = FormatGender(gender);
        var formattedBirthDate = FormatBirthDate(birthDate);
        var formattedPostalCode = FormatPostalCode(addressPostalCode);

        var data = PrepareDataString(formattedGiven, formattedFamily, formattedBirthDate, formattedGender, formattedPostalCode);
        return CreateHash(data);
    }

    public void StoreSearchIdInBaggage(SearchIdHash hash)
    {
        Activity.Current?.SetBaggage(SearchIdConstants.SearchIdStorageKey, hash.Value);
    }

    
}