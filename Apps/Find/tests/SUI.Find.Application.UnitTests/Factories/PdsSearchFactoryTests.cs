using SUI.Find.Application.Factories.PdsSearch;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.Application.UnitTests.Factories;

public class PdsSearchFactoryTests
{
    private class FakeStrategy : IPdsSearchStrategy
    {
        public int Version { get; }

        public OrderedDictionary<string, SearchQuery> BuildQuery(PersonSpecification model)
        {
            return new OrderedDictionary<string, SearchQuery>();
        }

        public FakeStrategy(int version) => Version = version;
    }

    [Fact]
    public void Should_Return_Strategy_For_Requested_Version()
    {
        var strategies = new List<IPdsSearchStrategy> { new FakeStrategy(1), new FakeStrategy(2) };

        var factory = new PdsSearchFactory(strategies);

        var s1 = factory.GetVersion(1);
        var s2 = factory.GetVersion(2);

        Assert.Equal(1, s1.Version);
        Assert.Equal(2, s2.Version);
    }

    [Fact]
    public void Should_Throw_NotSupported_When_Version_Not_Found()
    {
        var strategies = new List<IPdsSearchStrategy> { new FakeStrategy(1) };
        var factory = new PdsSearchFactory(strategies);

        var ex = Assert.Throws<NotSupportedException>(() => factory.GetVersion(999));
        Assert.Contains("PDS Search version 999 is not supported", ex.Message);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Duplicate_Versions_Provided()
    {
        var strategies = new List<IPdsSearchStrategy>
        {
            new FakeStrategy(1),
            new FakeStrategy(1), // duplicate key
        };

        Assert.Throws<ArgumentException>(() => new PdsSearchFactory(strategies));
    }

    [Fact]
    public void Should_Throw_ArgumentNullException_When_Strategies_Is_Null()
    {
        IEnumerable<IPdsSearchStrategy> strategies = null!;
        Assert.Throws<ArgumentNullException>(() => new PdsSearchFactory(strategies));
    }
}
