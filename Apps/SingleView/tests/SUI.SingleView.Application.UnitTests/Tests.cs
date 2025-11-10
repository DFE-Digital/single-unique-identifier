namespace SUI.SingleView.Application.UnitTests;

public class Tests
{
    [Fact]
    public void Test1()
    {
        var sut = new Class1();
        var result = sut.Add(1, 2);
        Assert.Equal(3, result);
    }
    
    [Fact]
    public void Test2()
    {
        var sut = new Class1();
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Add(-1, 2));
    }
}
