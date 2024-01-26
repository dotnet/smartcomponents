using TestMvcApp;

namespace SmartComponents.E2ETest.Mvc;

public class MvcSampleTest : SampleTest<Program>
{
    public MvcSampleTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }
}
