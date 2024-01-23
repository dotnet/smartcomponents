using ExampleMvcRazorPagesApp;

namespace SmartComponents.Blazor.Test;

public class BlazorSampleTest : SampleTest<Program>
{
    public BlazorSampleTest(CustomWebApplicationFactory<Program> server) : base(server)
    {
    }
}
