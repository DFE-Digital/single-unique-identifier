using Azure;
using Azure.Core;

namespace SUI.Find.Infrastructure.UnitTests.TestUtils;

public class MockAzureResponse : Response
{
    public override int Status => 200;
    public override string ReasonPhrase => "OK";
    public override Stream? ContentStream { get; set; }
    public override string ClientRequestId { get; set; } = "mock-request-id";
    public override void Dispose() { }
    protected override bool ContainsHeader(string name) => false;
    protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
    protected override bool TryGetHeader(string name, out string value) { value = string.Empty; return false; }
    protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values) { values = new List<string>(); return false; }
}