using Hl7.Fhir.Model;

namespace SUI.Find.Infrastructure.UnitTests.Stubs;

public static class StubFhirBundles
{
    public static Resource SinglePatient(string nhsNumber, decimal score)
    {
        var patient = new Patient
        {
            Id = nhsNumber
        };

        var bundleEntry = new Bundle.EntryComponent
        {
            Resource = patient,
            Search = new Bundle.SearchComponent
            {
                Score = score
            }
        };

        return new Bundle
        {
            Type = Bundle.BundleType.Searchset,
            Entry = new List<Bundle.EntryComponent> { bundleEntry }
        };
    }

    public static Bundle Empty() =>
        new()
        {
            Type = Bundle.BundleType.Searchset,
            Entry = []
        };

    public static Resource? MultiplePatients()
    {
        return new OperationOutcome()
        {
            Issue =
            [
                new OperationOutcome.IssueComponent()
                {
                    Code = OperationOutcome.IssueType.MultipleMatches
                }
            ]
        };
    }
}