using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Infrastructure.Fhir;
using SUI.Find.Infrastructure.Services;
using Task = System.Threading.Tasks.Task;

namespace SUI.Find.Infrastructure.UnitTests.Stubs;

public class BaseNhsFhirClientTests
{
    protected readonly ILogger<FhirService> LoggerMock;
    protected readonly IFhirClientFactory FhirClientFactoryMock;

    protected BaseNhsFhirClientTests()
    {
        LoggerMock = Substitute.For<ILogger<FhirService>>();
        FhirClientFactoryMock = Substitute.For<IFhirClientFactory>();
    }

    protected class TestFhirClientSinglePersonMatch(
        string endpoint = "https://example.com/fhir",
        FhirClientSettings settings = null!,
        HttpMessageHandler messageHandler = null!)
        : FhirClient(endpoint, settings, messageHandler)
    {
        public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
        {
            var bundle = new Bundle
            {
                Entry = new List<Bundle.EntryComponent>()
                {
                    new()
                    {
                        Resource = new Patient
                        {
                            Id = "123"
                        },
                        Search = new Bundle.SearchComponent()
                        {
                            Mode = Bundle.SearchEntryMode.Match,
                            Score = 1.0m
                        }
                    }
                },
            };

            return await Task.FromResult<Bundle?>(bundle);
        }

        public override Task<TResource?> ReadAsync<TResource>(Uri location, string? ifNoneMatch = null, DateTimeOffset? ifModifiedSince = null,
            CancellationToken? ct = null) where TResource : class
        {
            var resource = new Patient
            {
                Id = "123"
            } as TResource;
            return Task.FromResult(resource);
        }
    }

    protected class TestFhirClientMultiMatch(
        string endpoint = "https://example.com/fhir",
        FhirClientSettings settings = null!,
        HttpMessageHandler messageHandler = null!)
        : FhirClient(endpoint, settings, messageHandler)
    {
        public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
        {
            return await Task.FromResult<Bundle?>(null);
        }

        public override Resource? LastBodyAsResource => new OperationOutcome()
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

    protected class TestFhirClientUnmatched(
        string endpoint = "https://example.com/fhir",
        FhirClientSettings settings = null!,
        HttpMessageHandler messageHandler = null!)
        : FhirClient(endpoint, settings, messageHandler)
    {
        public override async Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
        {
            var bundle = new Bundle
            {
                Entry = []
            };

            return await Task.FromResult<Bundle?>(bundle);
        }
    }
}

public class TestFhirClientError : FhirClient
{
    public TestFhirClientError(string endpoint = "https://example.com/fhir", FhirClientSettings settings = null!, HttpMessageHandler messageHandler = null!) : base(endpoint, settings, messageHandler)
    {
    }

    public override Task<Bundle?> SearchAsync<TResource>(SearchParams q, CancellationToken? ct = null)
    {
        throw new Exception("Error occurred while performing search");
    }

    public override Task<TResource?> ReadAsync<TResource>(Uri location, string? ifNoneMatch = null, DateTimeOffset? ifModifiedSince = null,
        CancellationToken? ct = null) where TResource : class
    {

        throw new Exception("Error occurred while performing read");
    }
}