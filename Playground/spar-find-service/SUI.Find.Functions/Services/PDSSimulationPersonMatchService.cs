using Interfaces;
using Models;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services;

public sealed class PDSSimulationPersonMatchService : IPersonMatchService
{
    private readonly IReadOnlyList<PersonRecord> _people;

    public PDSSimulationPersonMatchService()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "Data", "pds-data.json");

        _people = LoadFromFile(path);
    }

    public string? FindExactNhsNumber(PersonSpecification input)
    {
        if (input is null)
        {
            return null;
        }

        var matches = _people
            .Select(p => new { Person = p, Score = Score(input, p) })
            .OrderByDescending(x => x.Score)
            .ToList();

        if (matches.Count == 0)
        {
            return null;
        }

        var best = matches[0];

        return best.Score == PerfectScore(input) ? best.Person.NhsNumber : null;
    }

    private static IReadOnlyList<PersonRecord> LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return Array.Empty<PersonRecord>();
        }

        var json = File.ReadAllText(path);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new DateOnlyJsonConverter());

        var collection = JsonSerializer.Deserialize<PersonRecordCollection>(json, options);

        return collection?.People ?? new List<PersonRecord>();
    }

    private static int Score(PersonSpecification input, PersonRecord candidate)
    {
        var score = 0;

        if (input.Given is not null)
        {
            if (NormaliseName(input.Given) == NormaliseName(candidate.Given))
            {
                score++;
            }
            else
            {
                return score;
            }
        }

        if (input.Family is not null)
        {
            if (NormaliseName(input.Family) == NormaliseName(candidate.Family))
            {
                score++;
            }
            else
            {
                return score;
            }
        }

        if (input.BirthDate is not null)
        {
            if (input.BirthDate == candidate.BirthDate)
            {
                score++;
            }
            else
            {
                return score;
            }
        }

        if (input.Gender is not null)
        {
            if (NormaliseGender(input.Gender) == NormaliseGender(candidate.Gender))
            {
                score++;
            }
            else
            {
                return score;
            }
        }

        if (input.Phone is not null)
        {
            if (NormalisePhone(input.Phone) == NormalisePhone(candidate.Phone))
            {
                score++;
            }
            else
            {
                return score;
            }
        }

        if (input.Email is not null)
        {
            if (NormaliseEmail(input.Email) == NormaliseEmail(candidate.Email))
            {
                score++;
            }
            else
            {
                return score;
            }
        }

        if (input.AddressPostalCode is not null)
        {
            if (NormalisePostcode(input.AddressPostalCode) == NormalisePostcode(candidate.AddressPostalCode))
            {
                score++;
            }
            else
            {
                return score;
            }
        }

        return score;
    }

    private static int PerfectScore(PersonSpecification input)
    {
        var perfect = 0;

        if (input.Given is not null) perfect++;
        if (input.Family is not null) perfect++;
        if (input.BirthDate is not null) perfect++;
        if (input.Gender is not null) perfect++;
        if (input.Phone is not null) perfect++;
        if (input.Email is not null) perfect++;
        if (input.AddressPostalCode is not null) perfect++;

        return perfect;
    }

    private static string? NormaliseName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToUpperInvariant();
    }

    private static string? NormaliseGender(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToUpperInvariant();
    }

    private static string? NormalisePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static string? NormaliseEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToUpperInvariant();
    }

    private static string? NormalisePostcode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().Replace(" ", string.Empty).ToUpperInvariant();
    }

    private sealed class DateOnlyJsonConverter : JsonConverter<DateOnly?>
    {
        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
            {
                return null;
            }

            if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                return d;
            }

            return DateOnly.Parse(s, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
    }
}