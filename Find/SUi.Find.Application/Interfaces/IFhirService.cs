using SUi.Find.Application.Models;

namespace SUi.Find.Application.Interfaces;

public interface IFhirService
{
    Task<FhirSearchResult> PerformSearchAsync();
}