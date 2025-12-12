using System.Text.Json;
using SUI.Transfer.Domain.SourceGenerated;
using Xunit.Abstractions;

namespace SUI.Transfer.Domain.Generator.Tests;

public class ConsolidatedFieldTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void ConsolidatedField_Does_RoundTrip_Json_Serialization_Test()
    {
        var input = new ConsolidatedField<string?>([
            new ConsolidatedFieldValue<string?>("Some text", "Provider1"),
            new ConsolidatedFieldValue<string?>(null, "Provider2"),
            new ConsolidatedFieldValue<string?>("Hello world", "Provider3"),
            new ConsolidatedFieldValue<string?>("Another example", "Provider4"),
        ]);

        // ACT
        var json = JsonSerializer.Serialize(input);
        var result = JsonSerializer.Deserialize<ConsolidatedField<string?>>(json);

        testOutputHelper.WriteLine("json: {0}", json);
        testOutputHelper.WriteLine("input: {0}", input);
        testOutputHelper.WriteLine("result: {0}", result);

        // ASSERT
        result.Should().NotBeSameAs(input);
        result.Should().BeEquivalentTo(input, options => options.WithStrictOrdering());
        result.Value.Should().Be("Some text");
    }
}
