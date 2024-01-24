using TestMvcApp;

namespace SmartComponents.Blazor.Test;

public class MvcSampleTest : SampleTest<Program>
{
    public MvcSampleTest(CustomWebApplicationFactory<Program> server) : base(server)
    {
    }
}
