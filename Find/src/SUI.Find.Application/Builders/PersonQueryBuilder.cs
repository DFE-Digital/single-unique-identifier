using SUi.Find.Application.Models;

namespace SUi.Find.Application.Builders;

/// <summary>
/// <para>Contains keys for different types of person search queries.</para>
/// <para>GFD = Given, Family, Date of Birth</para>
/// </summary>
public static class PersonQueryKeys
{
    public const string ExactGfd = "ExactGFD";
    public const string ExactAll = "ExactAll";
    public const string FuzzyGfd = "FuzzyGFD";
    public const string FuzzyAll = "FuzzyAll";
    public const string FuzzyGfdRange = "FuzzyGFDRange";
    public const string FuzzyAltDob = "FuzzyAltDob";
}

public static class PersonQueryBuilder
{
    private const string? DateFormat = "yyyy-MM-dd";

    public static OrderedDictionary<string, SearchQuery> CreateQueries(PersonSpecification model)
    {
        if (!model.BirthDate.HasValue)
        {
            throw new InvalidOperationException("Birthdate is required for search queries");
        }

        var dobRange = new[]
        {
            "ge" + model.BirthDate.Value.AddMonths(-6).ToString(DateFormat),
            "le" + model.BirthDate.Value.AddMonths(6).ToString(DateFormat)
        };
        var dob = new[] { "eq" + model.BirthDate.Value.ToString(DateFormat) };

        var modelName = model.Given is not null ? new[] { model.Given } : null;
        var queryOrderedMap = new OrderedDictionary<string, SearchQuery>
        {
            {
                PersonQueryKeys.ExactGfd, new SearchQuery() // exact search on only given, family and dob
                {
                    ExactMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Birthdate = dob
                }
            },
            {
                PersonQueryKeys.ExactAll, new SearchQuery() // 1. exact search
                {
                    ExactMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Email = model.Email,
                    Gender = model.Gender,
                    Phone = model.Phone,
                    Birthdate = dob,
                    AddressPostalcode = model.AddressPostalCode,
                }
            },
            {
                PersonQueryKeys.FuzzyGfd, new SearchQuery() // 2. fuzzy search on only given, family and dob
                {
                    FuzzyMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Birthdate = dob
                }
            },
            {
                PersonQueryKeys.FuzzyAll, new SearchQuery() // 3. fuzzy search with given name, family name and DOB.
                {
                    FuzzyMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Email = model.Email,
                    Gender = model.Gender,
                    Phone = model.Phone,
                    Birthdate = dob,
                    AddressPostalcode = model.AddressPostalCode,
                }
            },
            {
                PersonQueryKeys.FuzzyGfdRange, new SearchQuery() // 4. fuzzy search with given name, family name and DOB range 6 months either side of given date.
                {
                    FuzzyMatch = true, Given = modelName, Family = model.Family, Birthdate = dobRange,
                }
            },
        };

        // Only applicable if dob day is less than or equal to 12
        if (model.BirthDate.Value.Day <=
            12) // 5. fuzzy search with given name, family name and DOB. Day swapped with month if day equal to or less than 12.
        {
            var altDob = new DateTime(
                model.BirthDate.Value.Year,
                model.BirthDate.Value.Day,
                model.BirthDate.Value.Month,
                0, 0, 0,
                DateTimeKind.Unspecified
            );

            queryOrderedMap.Add(PersonQueryKeys.FuzzyAltDob, new SearchQuery
            {
                FuzzyMatch = true,
                Given = modelName,
                Family = model.Family,
                Email = model.Email,
                Gender = model.Gender,
                Phone = model.Phone,
                Birthdate = [$"eq{altDob:yyyy-MM-dd}"],
                AddressPostalcode = model.AddressPostalCode,
            });
        }

        return queryOrderedMap;
    }
}