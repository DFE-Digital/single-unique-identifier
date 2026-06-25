using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;

namespace SUI.Find.E2ETests;

public class E2ETestBase
{
    protected readonly FunctionTestFixture Fixture;

    protected readonly ITestOutputHelper TestOutputHelper;

    protected E2ETestBase(FunctionTestFixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        TestOutputHelper = testOutputHelper;

        TestOutputHelper.WriteLine(Fixture.Config.ToString());

        TestOutputHelper.WriteLine("Startup Diagnostic Messages:");
        TestOutputHelper.WriteLine(
            Fixture.StartupDiagnosticMessages == "" ? "None" : Fixture.StartupDiagnosticMessages
        );
    }
}
