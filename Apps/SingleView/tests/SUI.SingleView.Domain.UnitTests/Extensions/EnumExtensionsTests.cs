using Shouldly;
using SUI.SingleView.Domain.Extensions;

namespace SUI.SingleView.Domain.UnitTests.Extensions;

public class EnumExtensionsTests
{
    public enum TestEnum
    {
        Test,
        HelloWorld,
        XYZReferral,
        ThisIsATest,
        SomethingLongLikeThis,
        SomethingWithABCInTheMiddle,
        RecordV1,
        RecordV123,
        SomeCD67,
        Test123,
        Test123Test,
    }

    [Theory]
    [InlineData(TestEnum.Test, "Test")]
    [InlineData(TestEnum.HelloWorld, "Hello World")]
    [InlineData(TestEnum.XYZReferral, "XYZ Referral")]
    [InlineData(TestEnum.ThisIsATest, "This Is A Test")]
    [InlineData(TestEnum.SomethingLongLikeThis, "Something Long Like This")]
    [InlineData(TestEnum.SomethingWithABCInTheMiddle, "Something With ABC In The Middle")]
    [InlineData(TestEnum.RecordV1, "Record V1")]
    [InlineData(TestEnum.RecordV123, "Record V123")]
    [InlineData(TestEnum.SomeCD67, "Some CD67")]
    [InlineData(TestEnum.Test123, "Test 123")]
    [InlineData(TestEnum.Test123Test, "Test 123 Test")]
    public void ToFriendlyEnumString_Tests(TestEnum enumValue, string expectedResult)
    {
        enumValue.ToFriendlyEnumString().ShouldBe(expectedResult);
    }
}
