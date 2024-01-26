using TestBlazorApp;

namespace SmartComponents.E2ETest.Blazor;

public class BlazorSampleTest : SampleTest<Program>
{
    public BlazorSampleTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }
}
