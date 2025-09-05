using SUi.Find.Application.Models;

namespace SUi.Find.Application.Builders;

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
                "ExactGFD", new() // exact search on only given, family and dob
                {
                    ExactMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Birthdate = dob
                }
            },
            {
                "ExactAll", new() // 1. exact search
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
                "FuzzyGFD", new() // 2. fuzzy search on only given, family and dob
                {
                    FuzzyMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Birthdate = dob
                }
            },
            {
                "FuzzyAll", new() // 3. fuzzy search with given name, family name and DOB.
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
                "FuzzyGFDRange", new() // 4. fuzzy search with given name, family name and DOB range 6 months either side of given date.
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

            queryOrderedMap.Add("FuzzyAltDob", new SearchQuery
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