namespace SUI.Find.E2ETests;

public class FunctionTestFixture : IDisposable
{
    public readonly HttpClient Client = new() { BaseAddress = new Uri("http://localhost:7182") };

    public void Dispose()
    {
        // MAYBE: Delete everything in storage as a cleanup operation?
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
