using System.Text.RegularExpressions;
using SUI.Find.Application.Constants.Matching;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.Application.Services.PdsSearch;

public class SearchQueryBuilder
{
    private readonly OrderedDictionary<string, SearchQuery> _queries = new();
    private readonly PersonSpecification _model;
    private readonly int _dobRange;
    private readonly bool _preprocessNames;

    public SearchQueryBuilder(
        PersonSpecification model,
        int dobRange = 6,
        bool preprocessNames = false
    )
    {
        if (!model.BirthDate.HasValue)
        {
            throw new InvalidOperationException("Birthdate is required for search queries");
        }

        _model = model;
        _dobRange = dobRange;
        _preprocessNames = preprocessNames;
    }

    private string[]? ModelName => _model.Given is not null ? [_model.Given] : null;
    private string[]? ModelNames =>
        _model.Given?.Split(
            " ",
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
    private string? FamilyName =>
        _model.Family is not null
            ? Regex.Replace(
                _model.Family,
                @"\s\(.*\)",
                string.Empty,
                RegexOptions.Compiled,
                TimeSpan.FromMilliseconds(300)
            )
            : null;
    private string[] DobRange =>
        [
            "ge"
                + _model
                    .BirthDate!.Value.AddMonths(-_dobRange)
                    .ToString(PdsConstants.SearchQuery.DateFormat),
            "le"
                + _model
                    .BirthDate.Value.AddMonths(_dobRange)
                    .ToString(PdsConstants.SearchQuery.DateFormat),
        ];
    private string[] Dob =>
        ["eq" + _model.BirthDate!.Value.ToString(PdsConstants.SearchQuery.DateFormat)];

    /// <summary>
    /// See <see href="https://digital.nhs.uk/developer/api-catalogue/personal-demographics-service-fhir#get-/Patient"/>
    /// for postcode search details and using wildcard.
    /// </summary>
    /// <returns></returns>
    private string? PostcodeWildcard()
    {
        if (!string.IsNullOrEmpty(_model.AddressPostalCode))
        {
            var postcode =
                _model.AddressPostalCode.Length > 2
                    ? string.Concat(_model.AddressPostalCode.AsSpan(0, 2), "*")
                    : _model.AddressPostalCode;

            return postcode;
        }

        return null;
    }

    public void AddNonFuzzyGfd()
    {
        _queries.Add(
            "NonFuzzyGFD",
            new SearchQuery()
            {
                ExactMatch = false,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = Dob,
                History = true,
            }
        );
    }

    public void AddNonFuzzyGfdPostcode()
    {
        _queries.Add(
            "NonFuzzyGFDPostcode",
            new SearchQuery()
            {
                ExactMatch = false,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = Dob,
                AddressPostalcode = _model.AddressPostalCode,
                History = true,
            }
        );
    }

    public void AddNonFuzzyGfdRange()
    {
        _queries.Add(
            "NonFuzzyGFDRange",
            new SearchQuery()
            {
                ExactMatch = false,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = DobRange,
                History = true,
            }
        );
    }

    public void AddNonFuzzyGfdRangePostcode(bool usePostcodeWildcard = false)
    {
        var name = usePostcodeWildcard
            ? "NonFuzzyGFDRangePostcodeWildcard"
            : "NonFuzzyGFDRangePostcode";
        _queries.Add(
            name,
            new SearchQuery()
            {
                ExactMatch = false,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = DobRange,
                AddressPostalcode = usePostcodeWildcard
                    ? PostcodeWildcard()
                    : _model.AddressPostalCode,
                History = true,
            }
        );
    }

    public void AddNonFuzzyAll()
    {
        _queries.Add(
            "NonFuzzyAll",
            new SearchQuery()
            {
                ExactMatch = false,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Email = _model.Email,
                Gender = _model.Gender,
                Phone = _model.Phone,
                Birthdate = Dob,
                AddressPostalcode = _model.AddressPostalCode,
                History = true,
            }
        );
    }

    public void AddNonFuzzyAllPostcodeWildcard()
    {
        _queries.Add(
            "NonFuzzyAllPostcodeWildcard",
            new SearchQuery()
            {
                ExactMatch = false,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Email = _model.Email,
                Gender = _model.Gender,
                Phone = _model.Phone,
                Birthdate = Dob,
                AddressPostalcode = PostcodeWildcard(),
                History = true,
            }
        );
    }

    public void AddFuzzyGfd()
    {
        _queries.Add(
            "FuzzyGFD",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = Dob,
            }
        );
    }

    public void AddFuzzyAll()
    {
        _queries.Add(
            "FuzzyAll",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Email = _model.Email,
                Gender = _model.Gender,
                Phone = _model.Phone,
                Birthdate = Dob,
                AddressPostalcode = _model.AddressPostalCode,
            }
        );
    }

    public void AddFuzzyGfdPostcodeWildcard()
    {
        _queries.Add(
            "FuzzyGFDPostcodeWildcard",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = DobRange,
                AddressPostalcode = PostcodeWildcard(),
            }
        );
    }

    public void AddFuzzyGfdRangePostcodeWildcard()
    {
        _queries.Add(
            "FuzzyGFDRangePostcodeWildcard",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = DobRange,
                AddressPostalcode = PostcodeWildcard(),
            }
        );
    }

    public void AddFuzzyGfdRangePostcode()
    {
        _queries.Add(
            "FuzzyGFDRangePostcode",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = DobRange,
                AddressPostalcode = _model.AddressPostalCode,
            }
        );
    }

    public void AddFuzzyGfdRange()
    {
        _queries.Add(
            "FuzzyGFDRange",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = DobRange,
            }
        );
    }

    public void AddExactGfd()
    {
        _queries.Add(
            "ExactGFD",
            new SearchQuery()
            {
                ExactMatch = true,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Birthdate = Dob,
            }
        );
    }

    public void AddExactAll()
    {
        _queries.Add(
            "ExactAll",
            new SearchQuery()
            {
                ExactMatch = true,
                Given = _preprocessNames ? ModelNames : ModelName,
                Family = _preprocessNames ? FamilyName : _model.Family,
                Email = _model.Email,
                Gender = _model.Gender,
                Phone = _model.Phone,
                Birthdate = Dob,
                AddressPostalcode = _model.AddressPostalCode,
            }
        );
    }

    public void TryAddFuzzyAltDob()
    {
        if (_model.BirthDate?.Day <= 12)
        {
            var altDob = new DateTime(
                _model.BirthDate.Value.Year,
                _model.BirthDate.Value.Day,
                _model.BirthDate.Value.Month,
                0,
                0,
                0,
                DateTimeKind.Unspecified
            );

            _queries.Add(
                "FuzzyAltDob",
                new SearchQuery
                {
                    FuzzyMatch = true,
                    Given = _preprocessNames ? ModelNames : ModelName,
                    Family = _preprocessNames ? FamilyName : _model.Family,
                    Email = _model.Email,
                    Gender = _model.Gender,
                    Phone = _model.Phone,
                    Birthdate = [$"eq{altDob:yyyy-MM-dd}"],
                    AddressPostalcode = _model.AddressPostalCode,
                }
            );
        }
    }

    public OrderedDictionary<string, SearchQuery> Build()
    {
        return _queries;
    }
}
