namespace SUI.Find.Application.Abstractions;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
