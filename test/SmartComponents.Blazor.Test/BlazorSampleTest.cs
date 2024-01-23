using ExampleBlazorApp;

namespace SmartComponents.Blazor.Test;

public class BlazorSampleTest : SampleTest<ExampleBlazorApp.Program>
{
    public BlazorSampleTest(CustomWebApplicationFactory<Program> server) : base(server)
    {
    }
}
