namespace SUI.SingleView.Application;

public class Class1
{
    public int Add(int a, int b)
    {
        if (a < 0 || b < 0)
        {
            throw new ArgumentOutOfRangeException();
        }
        return a + b;
    }
}