namespace SUI.SingleView.Application.Models;

public class HttpPollingOptions
{
    public static string SectionName => "HttpPolling";

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan PollTimeout { get; init; } = TimeSpan.FromMinutes(1);
}
