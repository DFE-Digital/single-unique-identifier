namespace SUI.SingleView.Infrastructure.UnitTests;

public class Tests
{
    [Fact]
    public void Test1()
    {
        var sut = new Class1();
        var result = sut.Add(1, 2);
        Assert.Equal(3, result);
    }
}
