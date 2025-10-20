namespace SUi.Find.Application.Interfaces;

public interface ISearchIdService
{
    SearchIdHash CreatePersonHash(string? given, string? family, DateOnly? birthDate, string? gender, string? addressPostalCode);

    void StoreSearchIdInBaggage(SearchIdHash hash);
}

public record struct SearchIdHash(string Value);